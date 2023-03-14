using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Simulator : MonoBehaviour
{
    [SerializeField] private MainPresenter mainPresenter;
    [SerializeField] private FluidSolver fluidSolver;
    [SerializeField] private GridRenderer gridRenderer;
    [SerializeField] private CursorController cursorController;
    [SerializeField] private float performanceStatsUpdateInterval = 0.2f;
    [SerializeField] private float _gridWorldSize = 8.35f;

    private float performanceUpdateTimeWaited = 0f;
    private int simulationStepsPerSecond;
    private bool gridCleared = false;

    public float[,] StartingDensityGrid { get; set; }
    public float GridWorldSize { get { return _gridWorldSize; } }
    public int Fps { get; private set; }
    public int SimulationStepsPerSecond { get; private set; }
    public bool HasStarted { get; private set; }
    public bool IsPaused { get; set; }
    public int GridSize { get; private set; }
    public float CellSize { get; private set; }
    public bool NormalizeArrows { get; private set; }
    public float ArrowScale { get; private set; }
    public float DiffusionFactor { get; private set; }
    public int MinimumStepsPerSecond { get; private set; }
    public int GaussSeidelIterations { get; private set; }
    public float Cfl { get; private set; }
    public int CursorMode { get; private set; }
    public float CursorSize { get; private set; }
    public float CursorForce { get; private set; }

    private void Start()
    {
        mainPresenter.Initialize();
        SetStartValues();
        gridRenderer.SetupFirstTime();
        Initialize();
        cursorController.Initialize();

        IsPaused = true;
        HasStarted = false;

        SetStartValuesPostInitialization();
    }

    void SetStartValues()
    {
        GridSize = mainPresenter.GridSize.value;
        CellSize = GridWorldSize / GridSize;
        UpdateNormalizeArrows(mainPresenter.NormalizeArrows.value);
        UpdateArrowScale(mainPresenter.ArrowScale.value);
        UpdateDiffusionFactor(mainPresenter.DiffusionFactor.value);
        UpdateMinimumStepsPerSecond(mainPresenter.MinimumStepsPerSecond.value);
        UpdateGaussSeidelIterations(mainPresenter.GaussSeidelIterations.value);
        UpdateCfl(mainPresenter.Cfl.value);
        UpdateCursorMode(CursorController.CURSOR_MODE_ADD);
        UpdateCursorForce(mainPresenter.CursorForce.value);
    }

    void SetStartValuesPostInitialization()
    {
        UpdateDrawArrows(mainPresenter.DrawVelocityArrows.value);
        cursorController.UpdateRadius(mainPresenter.CursorSize.value);
    }

    public void Initialize(bool startingSimulation = false)
    {
        fluidSolver.Initialize();
        gridRenderer.Initialize();
        cursorController.UpdateAffectedCells();

        if (startingSimulation)
        {
            fluidSolver.DensityGrid = StartingDensityGrid;
        }
        else
        {
            StartingDensityGrid = new float[GridSize + 2, GridSize + 2];
        }
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.Escape))
        {
            Application.Quit();
        }

        UpdatePerformanceStats();

        if (IsPaused)
        {
            return;
        }

        fluidSolver.DoSimulationStep();
        gridRenderer.Render(fluidSolver.DensityGrid, fluidSolver.VelocityGridX, fluidSolver.VelocityGridY);
    }

    public void ToggleStartPause()
    {
        if (HasStarted)
        {
            IsPaused = !IsPaused;
            return;
        }

        StartSimulation();
    }

    public void StartSimulation()
    {
        HasStarted = true;
        IsPaused = false;
        fluidSolver.DensityGrid = StartingDensityGrid;
    }

    public void StopSimulation()
    {
        HasStarted = false;
        IsPaused = true;

        fluidSolver.DensityGrid = StartingDensityGrid;
        fluidSolver.ClearVelocityGrids();
        gridRenderer.Render(fluidSolver.DensityGrid, fluidSolver.VelocityGridX, fluidSolver.VelocityGridY);
    }

    public void ResetGrid()
    {
        StartingDensityGrid = new float[GridSize + 2, GridSize + 2];
        fluidSolver.DensityGrid = StartingDensityGrid;
        gridRenderer.Render(fluidSolver.DensityGrid, fluidSolver.VelocityGridX, fluidSolver.VelocityGridY);
    }

    public void RenderOnce()
    {
        gridRenderer.Render(fluidSolver.DensityGrid, fluidSolver.VelocityGridX, fluidSolver.VelocityGridY);
    }

    void UpdatePerformanceStats()
    {
        performanceUpdateTimeWaited += Time.deltaTime;

        if (performanceUpdateTimeWaited >= performanceStatsUpdateInterval)
        {
            Fps = Mathf.RoundToInt(1f / Time.deltaTime);
            simulationStepsPerSecond = Mathf.RoundToInt(1f / fluidSolver.TimeStep);
            mainPresenter.UpdatePerformanceStats(Fps, simulationStepsPerSecond);

            performanceUpdateTimeWaited = 0f;
        }

        if (IsPaused)
        {
            simulationStepsPerSecond = 0;
        }
    }

    public void TemporarilyChangeGridSize(int newGridSize)
    {
        if (!gridCleared)
        {
            fluidSolver.ClearDensityGrid();
            gridCleared = true;
        }

        GridSize = newGridSize;
        CellSize = GridWorldSize / GridSize;
        gridRenderer.DrawGridPreview();
    }

    public void FinalizeGridSize()
    {
        gridRenderer.ShowBorder();
        gridCleared = false;
        Initialize();
    }

    public void UpdateDrawArrows(bool onOrOff)
    {
        gridRenderer.SetArrowRendering(onOrOff);
    }

    public void UpdateNormalizeArrows(bool onOrOff)
    {
        gridRenderer.NormalizeArrows = onOrOff;
    }

    public void UpdateArrowScale(float newArrowScale)
    {
        gridRenderer.ArrowScale = newArrowScale;
    }

    public void UpdateDiffusionFactor(float newDiffusionFactor)
    {
        fluidSolver.DiffusionFactor = newDiffusionFactor;
    }

    public void UpdateMinimumStepsPerSecond(int newMinimumStepsPerSecond)
    {
        fluidSolver.MinimumStepsPerSecond = newMinimumStepsPerSecond;
    }

    public void UpdateGaussSeidelIterations(int newGaussSeidelIterations)
    {
        fluidSolver.GaussSeidelIterations = newGaussSeidelIterations;
    }

    public void UpdateCfl(float newCfl)
    {
        fluidSolver.Cfl = newCfl;
    }

    public void UpdateCursorMode(int mode)
    {
        cursorController.CursorMode = mode;
    }

    public void PrepareCirclePreview()
    {
        cursorController.PrepareCirclePreview();
    }

    public void UpdateCursorSize(float size)
    {
        cursorController.PreviewCircle(size);
    }

    public void FinishCirclePreview(float newRadius)
    {
        cursorController.FinishPreview(newRadius);
    }

    public void UpdateCursorForce(float newForce)
    {
        cursorController.MoveForce = newForce;
    }
}
