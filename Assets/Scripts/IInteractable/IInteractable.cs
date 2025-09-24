
interface IInteractable
{
    /// <summary>
    /// ใช้ครั้งเดียว
    /// </summary>
    void Interact();
    /// <summary>
    /// ใช้สำหรับ interact / cancel interact
    /// </summary>
    /// <param name="interact"></param>
    void Interact(bool interact);
    
/// <summary>
/// เช็คว่าตอนนี้ interact ได้หรือยัง
/// </summary>
    bool CanInteract { get; }
}
