using UnityEngine;

namespace LabQuimicaVR
{
    /// <summary>
    /// Puntero por mirada: lanza un rayo desde el centro de la vista. Lo que miras se resalta
    /// y al pulsar el boton del visor (o A del mando) se activa.
    /// </summary>
    public class GazePointer : MonoBehaviour
    {
        public static Transform Head { get; private set; }

        public Transform reticle;
        public float maxDistance = 40f;
        public float reticleScale = 0.012f;

        GazeTarget current;
        Renderer reticleRenderer;
        float pulse;

        void Awake()
        {
            Head = transform;
            if (reticle != null) reticleRenderer = reticle.GetComponentInChildren<Renderer>();
        }

        void Update()
        {
            var ray = new Ray(transform.position, transform.forward);
            GazeTarget target = null;
            float distance = 6f;
            if (Physics.Raycast(ray, out var hit, maxDistance, ~0, QueryTriggerInteraction.Collide))
            {
                distance = hit.distance;
                target = hit.collider.GetComponentInParent<GazeTarget>();
                if (target != null && !target.isActiveAndEnabled) target = null;
                if (target != null && !target.Interactable) target = null;
            }

            if (target != current)
            {
                if (current != null) current.HoverExit();
                current = target;
                if (current != null)
                {
                    current.HoverEnter();
                    LabAudio.Play(LabSound.Hover);
                }
            }

            if (LabInput.SelectDown && current != null && !ScreenFader.Busy)
            {
                pulse = 1f;
                LabAudio.Play(LabSound.Click);
                current.Click();
            }

            UpdateReticle(ray, distance);
        }

        void UpdateReticle(Ray ray, float distance)
        {
            if (reticle == null) return;
            pulse = Mathf.MoveTowards(pulse, 0f, Time.deltaTime * 4f);
            float d = Mathf.Max(0.3f, distance - 0.03f);
            reticle.position = ray.origin + ray.direction * d;
            reticle.rotation = Quaternion.LookRotation(ray.direction);
            float s = d * reticleScale * (current != null ? 1.8f : 1f) * (1f + pulse * 0.6f);
            reticle.localScale = new Vector3(s, s, s);
            if (reticleRenderer != null)
                reticleRenderer.material.color = current != null ? Palette.Accent : Color.white;
        }
    }
}
