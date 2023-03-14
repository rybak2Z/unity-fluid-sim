using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CursorController : MonoBehaviour
{
    [SerializeField] private FluidSolver fluidSolver;
    [SerializeField] private Simulator sim;
    [SerializeField] private int standardCircleStepCount = 10;
    [SerializeField] private int previewCircleStepCountFactor = 10;
    [SerializeField] private float width;
    [SerializeField] private float constantForceFactor = 5f;

    public const int CURSOR_MODE_MOVE = 0;
    public const int CURSOR_MODE_ADD = 1;
    public const int CURSOR_MODE_DELETE = 2;

    private LineRenderer lineRenderer;
    private Collider2D[] affectedCells;
    private int lastNumberAffected;
    private float radius;
    private Vector3[] circlePoints;
    private Vector3[] transformedCirclePoints;
    private Vector3 lastMousePosition;
    private bool inPreview = false;
    private bool inGrid;
    private bool inMovement = false;

    public int CursorMode { private get; set; }
    public float MoveForce { private get; set; }

    public void Initialize()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.startWidth = width;
        UpdateRadius(1f);
    }

    void Update()
    {
        Vector3 mouseWorldCoordinates = GetMouseWorldCoordinates();
        Vector3 distances = mouseWorldCoordinates - (transform.parent.position);
        distances.x = Mathf.Abs(distances.x);
        distances.y = Mathf.Abs(distances.y);

        inGrid = distances.x <= 0.5f * sim.GridWorldSize && distances.y <= 0.5f * sim.GridWorldSize;
        lineRenderer.enabled = inGrid || inPreview;

        if (inPreview)
        {
            return;
        }

        transform.position = mouseWorldCoordinates;
        DrawCircle();
        CheckInput();
    }

    void CheckInput()
    {
        switch (CursorMode)
        {
            case CURSOR_MODE_MOVE:
                CheckMoveAction();
                break;
            case CURSOR_MODE_ADD:
                CheckEditDensityAction(true);
                break;
            case CURSOR_MODE_DELETE:
                CheckEditDensityAction(false);
                break;
            default:
                Debug.LogWarning("Invalid Cursor Mode: " + CursorMode.ToString());
                break;
        }
    }

    void CheckMoveAction()
    {
        if (Input.GetMouseButtonUp(0) || !inGrid || sim.IsPaused)
        {
            inMovement = false;
            return;
        }

        if (Input.GetMouseButton(0))
        {
            if (!inMovement)
            {
                inMovement = true;
                lastNumberAffected = Physics2D.OverlapCircleNonAlloc(transform.position, radius - 0.5f * sim.CellSize, affectedCells);
                lastMousePosition = GetMouseWorldCoordinates();
                return;
            }

            // Calculate movement
            Vector3 currentMousePosiiton = GetMouseWorldCoordinates();
            Vector3 movement = currentMousePosiiton - lastMousePosition;
            movement *= MoveForce * constantForceFactor;
            fluidSolver.Forces = movement.magnitude;

            // Apply movement
            Vector2Int cellCoordinates;
            for (int i = 0; i < lastNumberAffected; i++)
            {
                cellCoordinates = affectedCells[i].GetComponent<FluidCell>().coordinates;
                fluidSolver.VelocityGridX[cellCoordinates.x + 1, cellCoordinates.y + 1] += movement.x;
                fluidSolver.VelocityGridY[cellCoordinates.x + 1, cellCoordinates.y + 1] += movement.y;
            }

            lastNumberAffected = Physics2D.OverlapCircleNonAlloc(transform.position, radius - 0.5f * sim.CellSize, affectedCells);
            lastMousePosition = currentMousePosiiton;
        }
    }

    void CheckEditDensityAction(bool addFluid)
    {
        if (!Input.GetMouseButton(0) || !inGrid)
        {
            return;
        }

        int numberOverlapping = Physics2D.OverlapCircleNonAlloc(transform.position, radius - 0.5f * sim.CellSize, affectedCells);
        Vector2Int cellCoordinates;
        for (int i = 0; i < numberOverlapping; i++)
        {
            cellCoordinates = affectedCells[i].GetComponent<FluidCell>().coordinates;
            fluidSolver.DensityGrid[cellCoordinates.x + 1, cellCoordinates.y + 1] = addFluid ? 1f : 0f;
            if (!sim.HasStarted)
            {
                sim.StartingDensityGrid[cellCoordinates.x + 1, cellCoordinates.y + 1] = addFluid ? 1f : 0f;
            }
        }

        if (sim.IsPaused)
        {
            sim.RenderOnce();
        }
    }

    public void UpdateRadius(float newRadius, bool isPreview = false)
    {
        radius = newRadius;

        // Prepare circle points
        int steps = (Mathf.RoundToInt(radius) + 1) * standardCircleStepCount;
        steps = isPreview ? steps * previewCircleStepCountFactor : steps;  // for preview, do more steps so it still looks good after upscaling
        lineRenderer.positionCount = steps;
        circlePoints = new Vector3[steps];
        transformedCirclePoints = new Vector3[steps];

        // Calculate circle points positions
        float circumferenceProgress;
        float radian;
        float x;
        float y;
        for (int currentStep = 0; currentStep < steps; currentStep++)  // only up to steps - 1 because the line renderer is set to loop
        {
            circumferenceProgress = ((float)currentStep / steps);
            radian = circumferenceProgress * 2 * Mathf.PI;
            x = Mathf.Cos(radian) * radius;
            y = Mathf.Sin(radian) * radius;
            circlePoints[currentStep] = new Vector3(x, y, 0f);
        }

        UpdateAffectedCells();
    }

    public void UpdateAffectedCells()
    {
        float diameter = 2 * radius;
        int numberAffectableCells = Mathf.RoundToInt(Mathf.Pow((diameter / sim.CellSize) * 1.5f, 2));  // Area of a square should be enough for area of a circle
        affectedCells = new Collider2D[numberAffectableCells];
    }

    public void PrepareCirclePreview()
    {
        lineRenderer.enabled = true;
        inPreview = true;
        transform.position = transform.parent.position;
        UpdateRadius(1f, true);
        PreviewCircle(radius);
    }

    public void PreviewCircle(float newRadius)
    {
        transform.localScale = new Vector3(newRadius, newRadius, newRadius);
        DrawCircle();
    }

    public void FinishPreview(float newRadius)
    {
        lineRenderer.enabled = false;
        inPreview = false;
        transform.localScale = Vector3.one;
        UpdateRadius(newRadius);
    }

    void DrawCircle()
    {
        for (int i = 0; i < circlePoints.Length; i++)
        {
            transformedCirclePoints[i] = transform.TransformPoint(circlePoints[i]);
        }

        lineRenderer.SetPositions(transformedCirclePoints);
    }

    Vector3 GetMouseWorldCoordinates()
    {
        Vector3 mouseScreenCoordinates = Input.mousePosition;
        mouseScreenCoordinates.z = Camera.main.nearClipPlane;
        return Camera.main.ScreenToWorldPoint(mouseScreenCoordinates);
    }
}
