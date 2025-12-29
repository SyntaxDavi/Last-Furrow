using UnityEngine;
public interface ISaveManager
{
    void SaveGame();
    void LoadGame();
    GameData Data { get; }
    void DeleteSave();
}

public interface IRunManager
{
    void StartNewRun();
    void AdvanceDay();
    bool IsRunActive { get; }
}

public interface IInteractable
{
    void OnClick();
    void OnHoverEnter();
    void OnHoverExit();
}
public interface IDropTarget
{
    bool CanReceive(IDraggable draggable);
    void OnReceive(IDraggable draggable);
}

public interface ICardReceiver
{
    bool CanReceiveCard(CardData card);
    void OnReceiveCard(CardData card);
}
public interface IDraggable
{
    void OnDragStart();

    void OnDragUpdate(Vector2 worldPosition);

    void OnDragEnd();
}