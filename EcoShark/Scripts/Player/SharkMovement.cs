using System.Collections.Generic;
using UnityEngine;

namespace EcoShark
{
    /// <summary>
    /// Movimiento 2.5D del tiburón guardián.
    ///  - Nada libre en 360° sobre el plano X/Y (la profundidad Z queda bloqueada).
    ///  - Acelera y frena suave (sensación de agua, no de patinaje).
    ///  - El modelo SIEMPRE apunta hacia donde se mueve (Quaternion.LookRotation),
    ///    y hace un "giro de barril" al cambiar de izquierda a derecha para no nadar panza arriba.
    ///  - Controles: WASD/Flechas/Mando, o mantener clic/dedo (estilo Hungry Shark). Shift = impulso.
    ///
    /// IMPORTANTE para tu modelo 3D: la "nariz" del tiburón debe mirar hacia +Z (flecha azul).
    /// Si tu modelo importado mira a otro lado, corrige con "Model Rotation Offset".
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class SharkMovement : MonoBehaviour
    {
        [Header("Velocidad")]
        [SerializeField] float maxSpeed = 7f;
        [Tooltip("Qué tan rápido alcanza la velocidad máxima (unidades/s²).")]
        [SerializeField] float acceleration = 18f;
        [Tooltip("Qué tan rápido frena al soltar los controles.")]
        [SerializeField] float deceleration = 9f;
        [SerializeField] float boostMultiplier = 1.6f;

        [Header("Rotación")]
        [Tooltip("Mayor = gira más rápido hacia la dirección de movimiento.")]
        [SerializeField] float turnSharpness = 10f;
        [Tooltip("Corrige la orientación si tu modelo 3D no mira hacia +Z al importarlo (ej. 0, 90, 0).")]
        [SerializeField] Vector3 modelRotationOffset = Vector3.zero;
        [Tooltip("Cuánto debe apuntar a la izquierda/derecha para voltear el lomo (evita parpadeo al nadar vertical).")]
        [SerializeField, Range(0.05f, 0.9f)] float flipThreshold = 0.25f;
        [Tooltip("Al quedarse quieto, el tiburón vuelve poco a poco a la horizontal.")]
        [SerializeField] float levelOutSpeed = 1.5f;

        [Header("Control con mouse / toque")]
        [SerializeField] bool allowPointerControl = true;
        [Tooltip("Distancia (en unidades) a la que el puntero da velocidad máxima.")]
        [SerializeField] float pointerFullSpeedDistance = 3f;
        [SerializeField] float pointerDeadZone = 0.4f;

        [Header("Plano de juego")]
        [Tooltip("Profundidad fija del plano de juego (normalmente 0).")]
        [SerializeField] float planeZ = 0f;

        [Header("Muerte")]
        [SerializeField] float deathSinkSpeed = 1.5f;

        Rigidbody rb;
        Camera cam;
        Vector2 input;
        bool boosting;
        bool facingRight = true;
        bool isDead;
        Vector3 lastMoveDir = Vector3.right;
        float upgradeSpeedMultiplier = 1f;
        readonly Dictionary<object, float> speedModifiers = new Dictionary<object, float>();

        /// <summary>Si es false, ignora los controles (pausa, cinemáticas, muerte).</summary>
        public bool CanMove { get; set; } = true;
        public bool IsBoosting => boosting;
        public Vector3 Velocity => rb != null ? GetVelocity() : Vector3.zero;
        public float MaxSpeed => maxSpeed * TotalSpeedMultiplier;

        float TotalSpeedMultiplier
        {
            get
            {
                float m = upgradeSpeedMultiplier;
                foreach (float mod in speedModifiers.Values) m *= mod;
                return m;
            }
        }

        // ------------------------------------------------------------------ Ciclo de vida

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;      // movimiento visual suave
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.constraints = RigidbodyConstraints.FreezePositionZ;     // bloquea la profundidad
            rb.sleepThreshold = 0f;                                    // que nunca "duerma" (triggers siempre activos)
            SetDamping(0f);                                            // el frenado lo controlamos nosotros

            Vector3 p = transform.position;
            p.z = planeZ;
            transform.position = p;

            upgradeSpeedMultiplier = Progression.SpeedMultiplier;
            cam = Camera.main;
        }

        void Update()
        {
            // La entrada se lee en Update (cada frame) y la física se aplica en FixedUpdate.
            if (!CanMove || isDead)
            {
                input = Vector2.zero;
                boosting = false;
                return;
            }

            input = ReadInput();
            boosting = input.sqrMagnitude > 0.01f && EcoInput.BoostHeld();
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            Vector3 current = GetVelocity();
            Vector3 target;
            float rate;

            if (isDead)
            {
                target = new Vector3(0f, -deathSinkSpeed, 0f);
                rate = deceleration;
            }
            else
            {
                float speed = MaxSpeed * (boosting ? boostMultiplier : 1f);
                target = new Vector3(input.x, input.y, 0f) * speed;
                rate = input.sqrMagnitude > 0.01f ? acceleration * (boosting ? boostMultiplier : 1f) : deceleration;
            }

            Vector3 newVelocity = Vector3.MoveTowards(current, target, rate * dt);
            newVelocity.z = 0f;
            SetVelocity(newVelocity);

            rb.angularVelocity = Vector3.zero; // la rotación la decide el código, no los choques
            RotateTowardsMovement(newVelocity, dt);
            KeepInsideOcean();
        }

        // ------------------------------------------------------------------ Entrada

        Vector2 ReadInput()
        {
            Vector2 keys = EcoInput.MoveAxis();
            if (keys.sqrMagnitude > 0.01f) return keys;

            if (allowPointerControl && EcoInput.PointerHeld(out Vector2 screenPos))
            {
                if (cam == null) cam = Camera.main;
                if (cam != null && TryGetPointOnPlane(screenPos, out Vector3 worldPoint))
                {
                    Vector2 delta = worldPoint - rb.position;
                    float distance = delta.magnitude;
                    if (distance < pointerDeadZone) return Vector2.zero;
                    // Más lejos el puntero = más rápido (hasta el máximo).
                    return delta / distance * Mathf.Clamp01(distance / pointerFullSpeedDistance);
                }
            }
            return Vector2.zero;
        }

        /// <summary>Convierte la posición del mouse/dedo en un punto del plano Z del juego.</summary>
        bool TryGetPointOnPlane(Vector2 screenPos, out Vector3 worldPoint)
        {
            Ray ray = cam.ScreenPointToRay(screenPos);
            Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, planeZ));
            if (plane.Raycast(ray, out float enter))
            {
                worldPoint = ray.GetPoint(enter);
                return true;
            }
            worldPoint = Vector3.zero;
            return false;
        }

        // ------------------------------------------------------------------ Rotación

        void RotateTowardsMovement(Vector3 velocity, float dt)
        {
            Vector3 dir;
            Vector3 up;

            if (isDead)
            {
                // Panza arriba: feedback claro de "game over".
                dir = facingRight ? Vector3.right : Vector3.left;
                up = Vector3.down;
            }
            else
            {
                if (velocity.sqrMagnitude > 0.05f)
                {
                    dir = velocity.normalized;
                }
                else
                {
                    // Quieto: se nivela suavemente hacia la horizontal.
                    Vector3 horizontal = facingRight ? Vector3.right : Vector3.left;
                    dir = Vector3.Slerp(lastMoveDir, horizontal, levelOutSpeed * dt).normalized;
                }
                lastMoveDir = dir;

                // ¿Mira a la derecha o a la izquierda? (con histéresis para no parpadear al nadar vertical)
                if (dir.x > flipThreshold) facingRight = true;
                else if (dir.x < -flipThreshold) facingRight = false;

                // Vector "lomo": perpendicular a la dirección, dentro del plano X/Y.
                // Cross(forward, dir) gira dir 90° en sentido antihorario.
                up = Vector3.Cross(Vector3.forward, dir);
                if (!facingRight) up = -up; // voltea el lomo para no nadar de cabeza
            }

            Quaternion targetRotation = Quaternion.LookRotation(dir, up) * Quaternion.Euler(modelRotationOffset);

            // Suavizado independiente del framerate.
            float t = 1f - Mathf.Exp(-turnSharpness * dt);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, t));
        }

        // ------------------------------------------------------------------ Límites

        void KeepInsideOcean()
        {
            OceanArea area = OceanArea.Current;
            if (area == null) return;

            Vector3 p = rb.position;
            Vector3 v = GetVelocity();
            bool changed = false;

            if (p.x < area.Min.x) { p.x = area.Min.x; if (v.x < 0f) v.x = 0f; changed = true; }
            if (p.x > area.Max.x) { p.x = area.Max.x; if (v.x > 0f) v.x = 0f; changed = true; }
            if (p.y < area.Min.y) { p.y = area.Min.y; if (v.y < 0f) v.y = 0f; changed = true; }
            if (p.y > area.Max.y) { p.y = area.Max.y; if (v.y > 0f) v.y = 0f; changed = true; }
            if (!Mathf.Approximately(p.z, planeZ)) { p.z = planeZ; changed = true; }

            if (changed)
            {
                rb.position = p;
                SetVelocity(v);
            }
        }

        // ------------------------------------------------------------------ API pública

        /// <summary>Aplica un multiplicador temporal (ej. petróleo = 0.45). "source" identifica quién lo puso.</summary>
        public void SetSpeedModifier(object source, float multiplier) => speedModifiers[source] = multiplier;

        public void RemoveSpeedModifier(object source) => speedModifiers.Remove(source);

        /// <summary>Empujón instantáneo (retroceso al recibir daño).</summary>
        public void ApplyImpulse(Vector3 velocityChange)
        {
            velocityChange.z = 0f;
            rb.AddForce(velocityChange, ForceMode.VelocityChange);
        }

        /// <summary>Lo llama el GameManager cuando la energía llega a 0.</summary>
        public void Die()
        {
            isDead = true;
            CanMove = false;
        }

        // ------------------------------------------------------------------ Compatibilidad Unity 2022 / Unity 6

        Vector3 GetVelocity()
        {
#if UNITY_6000_0_OR_NEWER
            return rb.linearVelocity;
#else
            return rb.velocity;
#endif
        }

        void SetVelocity(Vector3 v)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = v;
#else
            rb.velocity = v;
#endif
        }

        void SetDamping(float value)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearDamping = value;
#else
            rb.drag = value;
#endif
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, transform.forward * 2f); // hacia dónde "mira" el tiburón
        }
    }
}
