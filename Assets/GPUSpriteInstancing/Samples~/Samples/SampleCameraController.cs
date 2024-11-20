using UnityEngine;
using Unity.Mathematics;
using UnityEngine.EventSystems;

namespace GPUSpriteInstancing.Sample
{
    public class CameraController : MonoBehaviour
    {
        public Camera cam;

        [Header("Movement Settings")] public float dragSpeed = 2f;
        public float zoomSpeed = 1f;
        public float smoothTime = 0.15f;

        [Header("Zoom Limits")] public float minZoom = 0.5f;
        public float maxZoom = 20f;

        [Header("Bounds (Optional)")] public bool useBounds = false; // Changed default to false
        public float2 minBounds = new(-50f, -50f);
        public float2 maxBounds = new(50f, 50f);

        private Vector3 dragOrigin;
        private Vector2 targetPosition;
        private float targetZoom;
        private Vector2 currentVelocity;
        private float zoomVelocity;
        private float2 previousTouchDelta;
        private float previousTouchDistance;
        private bool isDragging;
        private const float MIN_PINCH_DISTANCE = 50f; // Minimum distance for pinch detection

        private void Start()
        {
            targetPosition = transform.position;
            targetZoom = cam.orthographicSize;
        }

        private void Update()
        {
            HandleInput();
            UpdateCameraTransform();
        }

        private void HandleInput()
        {
            // Handle desktop input
            if (Application.platform != RuntimePlatform.IPhonePlayer &&
                Application.platform != RuntimePlatform.Android)
            {
                HandleDesktopInput();
                return;
            }

            // Handle mobile input
            HandleMobileInput();
        }

        private void HandleDesktopInput()
        {
            // Mouse drag
            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

            // When starting drag
            if (Input.GetMouseButtonDown(0) && !overUI)
            {
                isDragging = true;
                dragOrigin = Input.mousePosition; // Store screen position directly
            }

            // During drag
            if (Input.GetMouseButton(0) && isDragging)
            {
                targetPosition +=
                    (Vector2)(cam.ScreenToWorldPoint(dragOrigin) - cam.ScreenToWorldPoint(Input.mousePosition)) *
                    dragSpeed;
                dragOrigin = Input.mousePosition;
            }

            // End drag
            if (Input.GetMouseButtonUp(0))
            {
                isDragging = false;
            }

            // Mouse scroll zoom remains unchanged
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (math.abs(scroll) > 0.01f)
            {
                targetZoom = math.clamp(
                    targetZoom - scroll * zoomSpeed * targetZoom,
                    minZoom,
                    maxZoom
                );
            }
        }

        private void HandleMobileInput()
        {
            if (Input.touchCount == 0)
            {
                isDragging = false;
                return;
            }

            // Single finger drag
            bool overUI = EventSystem.current != null &&
                          EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
            if (Input.touchCount == 1 && !overUI)
            {
                Touch touch = Input.GetTouch(0);

                if (touch.phase == TouchPhase.Began)
                {
                    isDragging = true;
                    dragOrigin = touch.position;
                }
                else if (touch.phase == TouchPhase.Moved && isDragging)
                {
                    targetPosition +=
                        (Vector2)(cam.ScreenToWorldPoint(dragOrigin) - cam.ScreenToWorldPoint(touch.position)) *
                        dragSpeed;
                    dragOrigin = touch.position;
                }
            }
            // Two finger pinch zoom
            else if (Input.touchCount == 2)
            {
                isDragging = false;
                Touch touch0 = Input.GetTouch(0);
                Touch touch1 = Input.GetTouch(1);

                if (touch0.phase == TouchPhase.Began || touch1.phase == TouchPhase.Began)
                {
                    previousTouchDistance = Vector2.Distance(touch0.position, touch1.position);
                    return;
                }

                float currentTouchDistance = Vector2.Distance(touch0.position, touch1.position);

                // Only process zoom if fingers are far enough apart
                if (currentTouchDistance > MIN_PINCH_DISTANCE)
                {
                    float deltaDistance = previousTouchDistance - currentTouchDistance;
                    targetZoom = math.clamp(
                        targetZoom + deltaDistance * zoomSpeed * 0.01f * targetZoom,
                        minZoom,
                        maxZoom
                    );

                    // Update position to zoom towards center point between fingers
                    if (math.abs(deltaDistance) > 1f)
                    {
                        Vector2 touchCenter = (touch0.position + touch1.position) * 0.5f;
                        Vector2 worldCenter = cam.ScreenToWorldPoint(touchCenter);
                        targetPosition = Vector2.Lerp(targetPosition, worldCenter, deltaDistance * 0.001f);
                    }
                }

                previousTouchDistance = currentTouchDistance;
            }
        }

        private void UpdateCameraTransform()
        {
            // Smooth position movement
            Vector2 currentPosition = transform.position;
            currentPosition = Vector2.SmoothDamp(
                currentPosition,
                ClampPosition(targetPosition),
                ref currentVelocity,
                smoothTime
            );

            // Smooth zoom
            float currentZoom = cam.orthographicSize;
            currentZoom = Mathf.SmoothDamp(
                currentZoom,
                targetZoom,
                ref zoomVelocity,
                smoothTime
            );

            // Apply transforms
            transform.position = new Vector3(currentPosition.x, currentPosition.y, transform.position.z);
            cam.orthographicSize = currentZoom;
        }

        private Vector2 ClampPosition(Vector2 position)
        {
            if (!useBounds) return position;

            // Adjust bounds based on zoom level to prevent seeing outside the bounds
            float vertExtent = cam.orthographicSize;
            float horzExtent = vertExtent * cam.aspect;

            float minX = minBounds.x + horzExtent;
            float maxX = maxBounds.x - horzExtent;
            float minY = minBounds.y + vertExtent;
            float maxY = maxBounds.y - vertExtent;

            float newX = math.clamp(position.x, minX, maxX);
            float newY = math.clamp(position.y, minY, maxY);

            return new Vector2(newX, newY);
        }

        private void OnDrawGizmosSelected()
        {
            if (!useBounds) return;

            // Draw bounds in editor
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(
                new Vector3((minBounds.x + maxBounds.x) * 0.5f, (minBounds.y + maxBounds.y) * 0.5f, 0f),
                new Vector3(maxBounds.x - minBounds.x, maxBounds.y - minBounds.y, 0f)
            );
        }
    }
}