using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MainPresenter : MonoBehaviour
{
    [SerializeField] Simulator sim;

    public Label Fps { get; private set; }
    public Label StepsPerSecond { get; private set; }
    public SliderInt GridSize { get; private set; }
    public Toggle DrawVelocityArrows { get; private set; }
    public Toggle NormalizeArrows { get; private set; }
    public Slider ArrowScale { get; private set; }
    public Slider DiffusionFactor { get; private set; }
    public SliderInt MinimumStepsPerSecond { get; private set; }
    public SliderInt GaussSeidelIterations { get; private set; }
    public Slider Cfl { get; private set; }
    public RadioButton CursorModeMove { get; private set; }
    public RadioButton CursorModeDraw { get; private set; }
    public RadioButton CursorModeDelete { get; private set; }
    public Slider CursorSize { get; private set; }
    public Slider CursorForce { get; private set; }
    public Button ResetGridButton { get; private set; }
    public Button StartPauseButton { get; private set; }
    public Button StopButton { get; private set; }

    public void Initialize()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        QueryVisualElements(root);
        RegisterCallbacks();
    }

    void QueryVisualElements(VisualElement root)
    {
        Fps = root.Q<Label>("FPS");
        StepsPerSecond = root.Q<Label>("StepsPerSecond");
        GridSize = root.Q<SliderInt>("GridSize");
        DrawVelocityArrows = root.Q<Toggle>("DrawVelocityArrows");
        NormalizeArrows = root.Q<Toggle>("NormalizeArrows");
        ArrowScale = root.Q<Slider>("ArrowScale");
        DiffusionFactor = root.Q<Slider>("DiffusionFactor");
        MinimumStepsPerSecond = root.Q<SliderInt>("MinimumStepsPerSecond");
        GaussSeidelIterations = root.Q<SliderInt>("GaussSeidelIterations");
        Cfl = root.Q<Slider>("CFL");
        CursorModeMove = root.Q<RadioButton>("Move");
        CursorModeDraw = root.Q<RadioButton>("DrawSource");
        CursorModeDelete = root.Q<RadioButton>("Delete");
        CursorSize = root.Q<Slider>("CursorSize");
        CursorForce = root.Q<Slider>("CursorForce");
        ResetGridButton = root.Q<Button>("ResetGrid");
        StartPauseButton = root.Q<Button>("Start");
        StopButton = root.Q<Button>("Stop");
    }

    void RegisterCallbacks()
    {
        GridSize.RegisterValueChangedCallback(v => { sim.TemporarilyChangeGridSize(v.newValue); });
        GridSize.RegisterCallback<ClickEvent>((e) => { sim.FinalizeGridSize(); });
        DrawVelocityArrows.RegisterValueChangedCallback(v => sim.UpdateDrawArrows(v.newValue));
        NormalizeArrows.RegisterValueChangedCallback(v => sim.UpdateNormalizeArrows(v.newValue));
        ArrowScale.RegisterValueChangedCallback(v => sim.UpdateArrowScale(v.newValue));
        DiffusionFactor.RegisterValueChangedCallback(v => sim.UpdateDiffusionFactor(v.newValue));
        MinimumStepsPerSecond.RegisterValueChangedCallback(v => sim.UpdateMinimumStepsPerSecond(v.newValue));
        GaussSeidelIterations.RegisterValueChangedCallback(v => sim.UpdateGaussSeidelIterations(v.newValue));
        Cfl.RegisterValueChangedCallback(v => sim.UpdateCfl(v.newValue));
        CursorModeMove.RegisterValueChangedCallback(v => { if (v.newValue) { sim.UpdateCursorMode(CursorController.CURSOR_MODE_MOVE); } });
        CursorModeDraw.RegisterValueChangedCallback(v => { if (v.newValue) { sim.UpdateCursorMode(CursorController.CURSOR_MODE_ADD); } });
        CursorModeDelete.RegisterValueChangedCallback(v => { if (v.newValue) { sim.UpdateCursorMode(CursorController.CURSOR_MODE_DELETE); } });
        CursorSize.RegisterValueChangedCallback(v => sim.UpdateCursorSize(v.newValue));
        CursorSize.RegisterCallback<MouseDownEvent>((e) => { sim.PrepareCirclePreview(); });
        CursorSize.RegisterCallback<ClickEvent>((e) => { sim.FinishCirclePreview(CursorSize.value); });
        CursorForce.RegisterValueChangedCallback(v => sim.UpdateCursorForce(v.newValue));
        ResetGridButton.clicked += () =>
        {
            sim.ResetGrid();
            GridSize.SetEnabled(true);
        };
        StartPauseButton.clicked += () =>
        {
            sim.ToggleStartPause();
            GridSize.SetEnabled(false);
            ResetGridButton.SetEnabled(false);
        };
        StopButton.clicked += () =>
        {
            sim.StopSimulation();
            ResetGridButton.SetEnabled(true);
        };
    }

    public void UpdatePerformanceStats(int fps, int stepsPerSecond)
    {
        Fps.text = "FPS: " + fps.ToString();
        StepsPerSecond.text = "Steps/s: " + stepsPerSecond.ToString();
    }
}
