using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TouchInputRouter : MonoBehaviour
{
    public GameManager gameManager;

    private class ActiveDrag
    {
        public int fingerId;
        public PieceController piece;
    }

    private const int MouseFingerId = -1;
    private readonly List<ActiveDrag> activeDrags = new List<ActiveDrag>();

    void Update()
    {
        if (Input.touchSupported && Input.touchCount > 0)
        {
            foreach (var touch in Input.touches)
                HandlePointer(touch.fingerId, touch.position, touch.phase);
        }
        else
        {
            if (Input.GetMouseButtonDown(0)) HandlePointer(MouseFingerId, Input.mousePosition, TouchPhase.Began);
            else if (Input.GetMouseButton(0)) HandlePointer(MouseFingerId, Input.mousePosition, TouchPhase.Moved);
            else if (Input.GetMouseButtonUp(0)) HandlePointer(MouseFingerId, Input.mousePosition, TouchPhase.Ended);
        }
    }

    private bool IsOverUI(int fingerId)
    {
        if (EventSystem.current == null) return false;
        return fingerId == MouseFingerId
            ? EventSystem.current.IsPointerOverGameObject()
            : EventSystem.current.IsPointerOverGameObject(fingerId);
    }

    private void HandlePointer(int fingerId, Vector2 screenPos, TouchPhase phase)
    {
        switch (phase)
        {
            case TouchPhase.Began:
                if (IsOverUI(fingerId)) return;
                HandleBegan(fingerId, screenPos);
                break;
            case TouchPhase.Moved:
            case TouchPhase.Stationary:
                HandleMoved(fingerId, screenPos);
                break;
            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                HandleEnded(fingerId);
                break;
        }
    }

    private Vector3 ScreenToWorld(Vector2 screenPos)
    {
        var cam = gameManager.mainCam;
        Vector3 screen = new Vector3(screenPos.x, screenPos.y, -cam.transform.position.z);
        Vector3 world = cam.ScreenToWorldPoint(screen);
        world.z = 0f;
        return world;
    }

    private void HandleBegan(int fingerId, Vector2 screenPos)
    {
        if (gameManager == null || gameManager.mainCam == null) return;
        if (activeDrags.Exists(d => d.fingerId == fingerId)) return;

        Vector3 world = ScreenToWorld(screenPos);
        var hit = Physics2D.OverlapPoint(world);
        var piece = hit != null ? hit.GetComponent<PieceController>() : null;

        if (piece != null && !activeDrags.Exists(d => d.piece == piece))
        {
            piece.BeginDrag(world);
            activeDrags.Add(new ActiveDrag { fingerId = fingerId, piece = piece });
        }
    }

    private void HandleMoved(int fingerId, Vector2 screenPos)
    {
        if (gameManager == null || gameManager.mainCam == null) return;

        var drag = activeDrags.Find(d => d.fingerId == fingerId);
        if (drag != null)
            drag.piece.UpdateDrag(ScreenToWorld(screenPos));
    }

    private void HandleEnded(int fingerId)
    {
        var drag = activeDrags.Find(d => d.fingerId == fingerId);
        if (drag != null)
        {
            drag.piece.EndDrag();
            activeDrags.Remove(drag);
        }
    }
}
