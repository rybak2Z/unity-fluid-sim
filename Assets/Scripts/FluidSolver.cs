using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class FluidSolver : MonoBehaviour
{
    [SerializeField] private Simulator sim;

    private const int BOUND_MODE_NONE = 0;
    private const int BOUND_MODE_VERTICAL = 1;
    private const int BOUND_MODE_HORIZONTAL = 2;

    private int stepsPerSecondLimit = 500;
    private float maxTimeStep;
    private float currentVMax = 100f;
    private float timeWaited = 0f;

    public float[,] DensityGrid { get; set; }
    public float[,] VelocityGridX { get; set; }
    public float[,] VelocityGridY { get; set; }
    public float TimeStep { get; private set; }
    public float DiffusionFactor { private get; set; }
    public int MinimumStepsPerSecond { private get; set; }
    public int GaussSeidelIterations { private get; set; }
    public float Cfl { private get; set; }
    public float Forces { private get; set; }

    public void Initialize()
    {
        DensityGrid = new float[sim.GridSize + 2, sim.GridSize + 2];
        VelocityGridX = new float[sim.GridSize + 2, sim.GridSize + 2];
        VelocityGridY = new float[sim.GridSize + 2, sim.GridSize + 2];

        maxTimeStep = 1f / MinimumStepsPerSecond;
        TimeStep = maxTimeStep;
    }

    public void DoSimulationStep()
    {
        // Terminate if required steps per second too high
        if (1f / TimeStep > stepsPerSecondLimit)
        {
            Debug.LogError("Script disabled due to the required steps per second surpassing the limit of " + stepsPerSecondLimit.ToString());
            return;
        }

        timeWaited += Time.deltaTime;
        while (timeWaited > 0)
        {
            TimeStep = Cfl * (sim.CellSize / currentVMax);
            TimeStep = Mathf.Clamp(TimeStep, TimeStep, maxTimeStep);

            // Velocity step
            VelocityGridX = Diffuse(VelocityGridX, BOUND_MODE_HORIZONTAL);
            VelocityGridY = Diffuse(VelocityGridY, BOUND_MODE_VERTICAL);
            Project();
            VelocityGridX = Advect(VelocityGridX, BOUND_MODE_HORIZONTAL);
            VelocityGridY = Advect(VelocityGridY, BOUND_MODE_VERTICAL);
            Project();

            // Density step
            DensityGrid = Diffuse(DensityGrid, BOUND_MODE_NONE);
            DensityGrid = Advect(DensityGrid, BOUND_MODE_NONE);

            UpdateMaxVelocity();
            timeWaited -= TimeStep;
        }
    }

    float[,] Diffuse(float[,] grid, int boundaryMode) {
        float changeFactor = DiffusionFactor * TimeStep;
        float[,] newGrid = new float[sim.GridSize + 2, sim.GridSize + 2];

        float surroundingAverage;
        for (int i = 0; i < GaussSeidelIterations; i++)
        {
            for (int x = 1; x < sim.GridSize + 1; x++)
            {
                for (int y = 1; y < sim.GridSize + 1; y++)
                {
                    surroundingAverage = (newGrid[x + 1, y] + newGrid[x - 1, y] + newGrid[x, y + 1] + newGrid[x, y - 1]) * 0.25f;
                    newGrid[x, y] = (grid[x, y] + changeFactor * surroundingAverage) / (1 + changeFactor);
                }
            }

            newGrid = SetBoundaryCondition(newGrid, boundaryMode);
        }

        return newGrid;
    }

    float[,] SetBoundaryCondition(float[,] grid, int boundaryMode)
    {
        int factorHorizontal;
        int factorVertical;

        switch (boundaryMode)
        {
            case BOUND_MODE_NONE:
                factorHorizontal = 1;
                factorVertical = 1;
                break;
            case BOUND_MODE_HORIZONTAL:
                factorHorizontal = -1;
                factorVertical = 1;
                break;
            case BOUND_MODE_VERTICAL:
                factorHorizontal = 1;
                factorVertical = -1;
                break;
            default:
                Debug.LogWarning("Warning: Invalid value for parameter 'mode'");
                return grid;
        }

        for (int i = 1; i < sim.GridSize + 1; i++)
        {
            grid[0, i] = grid[1, i] * factorHorizontal;
            grid[sim.GridSize + 1, i] = grid[sim.GridSize, i] * factorHorizontal;
            grid[i, 0] = grid[i, 1] * factorVertical;
            grid[i, sim.GridSize + 1] = grid[i, sim.GridSize] * factorVertical;
        }

        grid[0, 0] = 0.5f * (grid[1, 0] + grid[0, 1]);
        grid[0, sim.GridSize + 1] = 0.5f * (grid[0, sim.GridSize] + grid[1, sim.GridSize + 1]);
        grid[sim.GridSize + 1, 0] = 0.5f * (grid[sim.GridSize, 0] + grid[sim.GridSize + 1, 1]);
        grid[sim.GridSize + 1, sim.GridSize + 1] = 0.5f * (grid[sim.GridSize, sim.GridSize + 1] + grid[sim.GridSize + 1, sim.GridSize]);

        return grid;
    }

    float[,] Advect(float[,] grid, int boundaryMode) {
        float[,] newGrid = new float[sim.GridSize + 2, sim.GridSize + 2];

        float originX;
        float originY;
        int originFloorX;
        int originFloorY;
        float originFractionalX;
        float originFractionalY;
        float valueBottomRow;
        float valueTopRow;

        for (int x = 1; x < sim.GridSize + 1; x++)
        {
            for (int y = 1; y < sim.GridSize + 1; y++)
            {
                // Calculate origin
                originX = x - TimeStep * VelocityGridX[x, y];
                originY = y - TimeStep * VelocityGridY[x, y];
                originX = Mathf.Clamp(originX, 0.5f, sim.GridSize + 0.5f);
                originY = Mathf.Clamp(originY, 0.5f, sim.GridSize + 0.5f);

                // Get whole number and fractional part or origin coordinates
                originFloorX = Mathf.FloorToInt(originX);
                originFloorY = Mathf.FloorToInt(originY);
                originFractionalX = originX - originFloorX;
                originFractionalY = originY - originFloorY;

                // Linearly interpolate surrounding values to get new value
                valueBottomRow = Lerp(grid[originFloorX, originFloorY], grid[originFloorX + 1, originFloorY], originFractionalX);
                valueTopRow = Lerp(grid[originFloorX, originFloorY + 1], grid[originFloorX + 1, originFloorY + 1], originFractionalX);
                newGrid[x, y] = Lerp(valueBottomRow, valueTopRow, originFractionalY);
            }
        }

        newGrid = SetBoundaryCondition(newGrid, boundaryMode);
        return newGrid;
    }

    float Lerp(float a, float b, float k)
    {
        return a + k * (b - a);
    }

    void Project()
    {
        float[,] divergenceGrid = new float[sim.GridSize + 2, sim.GridSize + 2];

        float velocityDifferenceX;
        float velocityDifferenceY;

        // Calculate divergence
        for (int x = 1; x < sim.GridSize + 1; x++)
        {
            for (int y = 1; y < sim.GridSize + 1; y++)
            {
                velocityDifferenceX = VelocityGridX[x + 1, y] - VelocityGridX[x - 1, y];
                velocityDifferenceY = VelocityGridY[x, y + 1] - VelocityGridY[x, y - 1];
                divergenceGrid[x, y] = -0.5f * (velocityDifferenceX + velocityDifferenceY);
            }
        }
        divergenceGrid = SetBoundaryCondition(divergenceGrid, BOUND_MODE_NONE);

        // Solve Poisson equation with Gauss-Seidel
        float[,] poissonGrid = new float[sim.GridSize + 2, sim.GridSize + 2];
        float surroundingSumP;
        for (int i = 0; i < GaussSeidelIterations; i++)
        {
            for (int x = 1; x < sim.GridSize + 1; x++)
            {
                for (int y = 1; y < sim.GridSize + 1; y++)
                {
                    surroundingSumP = poissonGrid[x - 1, y] + poissonGrid[x + 1, y] + poissonGrid[x, y - 1] + poissonGrid[x, y + 1];
                    poissonGrid[x, y] = (surroundingSumP + divergenceGrid[x, y]) / 4f;
                }
            }
            poissonGrid = SetBoundaryCondition(poissonGrid, BOUND_MODE_NONE);
        }

        // Subtract curl-free grid from velocity grids to make them divergence-free
        for (int x = 1; x < sim.GridSize + 1; x++)
        {
            for (int y = 1; y < sim.GridSize + 1; y++)
            {
                VelocityGridX[x, y] -= 0.5f * (poissonGrid[x + 1, y] - poissonGrid[x - 1, y]);
                VelocityGridY[x, y] -= 0.5f * (poissonGrid[x, y + 1] - poissonGrid[x, y - 1]);
            }
        }

        VelocityGridX = SetBoundaryCondition(VelocityGridX, BOUND_MODE_HORIZONTAL);
        VelocityGridY = SetBoundaryCondition(VelocityGridY, BOUND_MODE_VERTICAL);
    }

    void UpdateMaxVelocity()
    {
        float maxVelocitySquared = 0f;
        float newVelocitySquared;

        for (int x = 1; x < sim.GridSize + 1; x++)
        {
            for (int y = 1; y < sim.GridSize + 1; y++)
            {
                newVelocitySquared = VelocityGridX[x, y] * VelocityGridX[x, y] + VelocityGridY[x, y] * VelocityGridY[x, y];
                maxVelocitySquared = newVelocitySquared > maxVelocitySquared ? newVelocitySquared : maxVelocitySquared;
            }
        }

        currentVMax = Mathf.Sqrt(maxVelocitySquared) + Mathf.Sqrt(sim.CellSize * Forces);
    }

    public void ClearDensityGrid()
    {
        for (int x = 1; x < sim.GridSize + 1; x++)
        {
            for (int y = 1; y < sim.GridSize + 1; y++)
            {
                DensityGrid[x, y] = 0f;
            }
        }
    }

    public void ClearVelocityGrids()
    {
        for (int x = 1; x < sim.GridSize + 1; x++)
        {
            for (int y = 1; y < sim.GridSize + 1; y++)
            {
                VelocityGridX[x, y] = 0f;
                VelocityGridY[x, y] = 0f;
            }
        }
    }

    void DEBUG_InsertTestValues()
    {
        // Put fluid source in center
        int centerCoordinate = sim.GridSize / 2;
        DensityGrid[centerCoordinate, centerCoordinate] = 1f;
        VelocityGridY[centerCoordinate, centerCoordinate] = 17f;

        // Put velocity to the left in the top right
        int topRightCoordinate = Mathf.RoundToInt(0.75f * sim.GridSize);
        VelocityGridX[topRightCoordinate, topRightCoordinate] = -7f;
    }
}
