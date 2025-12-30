using System;
public class CameraEvents
{
    // Disparado sempre que a câmera termina de mover um frame ou muda zoom
    public event Action OnCameraUpdated;

    public void TriggerCameraUpdated() => OnCameraUpdated?.Invoke();
}