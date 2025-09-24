using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using TMPro;

public class CharacterSelectManager : NetworkBehaviour
{
    [System.Serializable]
    public class CharacterAnchors
    {
        public RectTransform hostAnchor;
        public RectTransform clientAnchor;
    }

    [System.Serializable]
    public class ConfirmUI
    {
        [Header("Container (optional)")]
        public GameObject container;       // Panel ครอบปุ่ม/ไอคอน (ถ้ามี)

        [Header("Button (local only)")]
        public Button button;              // ปุ่มกดยืนยัน (โผล่เฉพาะฝั่งตัวเอง)
        public TMP_Text label;             // ข้อความบนปุ่ม ("Confirm")

        [Header("Ready Icon (shown to both sides when ready)")]
        public Image iconReady;            // ไอคอนติ๊กถูก (เห็นทั้งสองฝั่งเมื่อพร้อม)
    }

    [Header("Slot Panels (UI)")]
    [SerializeField] private RectTransform hostSlotPanel;
    [SerializeField] private RectTransform clientSlotPanel;

    [Header("Arrow Buttons")]
    [SerializeField] private Button hostLeftBtn;
    [SerializeField] private Button hostRightBtn;
    [SerializeField] private Button clientLeftBtn;
    [SerializeField] private Button clientRightBtn;

    [Header("Confirm Controls")]
    [SerializeField] private ConfirmUI hostConfirm;
    [SerializeField] private ConfirmUI clientConfirm;

    [Header("Join Code UI (optional)")]
    [SerializeField] private TMP_Text joinCodeLabel;
    [SerializeField] private Button copyBtn;

    [Header("Per-Character Anchors (index = character)")]
    [SerializeField] private CharacterAnchors[] characterAnchors;

    [Header("Animation")]
    [SerializeField] private float moveDuration = 0.5f;

    [Header("Scene")]
    [SerializeField] private string gameplaySceneName = "Gameplay";

    [Header("Debug")]
    [SerializeField] private bool verboseLog = true;

    private LobbySelectionState S;
    private bool subscribed;
    private int CharacterCount => characterAnchors != null ? characterAnchors.Length : 0;

    // ───────── Lifecycle ─────────
    public override void OnNetworkSpawn()
    {
        if (verboseLog) Debug.Log("[CSM] OnNetworkSpawn()");
        AutoWireIfNeeded();
        TryBind();
        Refresh();

        if (LobbySelectionState.I != null)
            LobbySelectionState.I.joinCode.OnValueChanged += (_, __) => UpdateJoinCodeUI();

        if (copyBtn) copyBtn.onClick.AddListener(CopyJoinCode);

        // เริ่มต้น: ซ่อนไอคอนพร้อมไว้ก่อน
        SetReadyIcon(hostConfirm, false);
        SetReadyIcon(clientConfirm, false);

        UpdateJoinCodeUI();
        PositionPanelsToCurrent();
    }

    private void OnEnable()
    {
        AutoWireIfNeeded();
        UpdateJoinCodeUI();
    }

    private void OnDisable() => Unsubscribe();

    // ───────── Auto-wire ─────────
    private void AutoWireIfNeeded()
    {
        // Host
        if (!hostConfirm.button) hostConfirm.button = FindButton("ConfirmBtn_Host");
        if (!hostConfirm.label && hostConfirm.button) hostConfirm.label = hostConfirm.button.GetComponentInChildren<TMP_Text>(true);
        if (!hostConfirm.iconReady) hostConfirm.iconReady = FindImage("IconReady_Host");

        // Client
        if (!clientConfirm.button) clientConfirm.button = FindButton("ConfirmBtn_Client");
        if (!clientConfirm.label && clientConfirm.button) clientConfirm.label = clientConfirm.button.GetComponentInChildren<TMP_Text>(true);
        if (!clientConfirm.iconReady) clientConfirm.iconReady = FindImage("IconReady_Client");

        if (!joinCodeLabel) joinCodeLabel = FindTMP("JoinCodeLabel");
        if (!copyBtn) copyBtn = FindButton("Button_CopyCode");

        if (verboseLog)
            Debug.Log($"[CSM] AutoWire OK -> hostBtn={(hostConfirm.button ? hostConfirm.button.name : "null")}," +
                      $" clientBtn={(clientConfirm.button ? clientConfirm.button.name : "null")}," +
                      $" hostIcon={(hostConfirm.iconReady ? hostConfirm.iconReady.name : "null")}," +
                      $" clientIcon={(clientConfirm.iconReady ? clientConfirm.iconReady.name : "null")}," +
                      $" anchors={CharacterCount}");
    }

    private Button FindButton(string name) { var go = GameObject.Find(name); return go ? go.GetComponent<Button>() : null; }
    private TMP_Text FindTMP(string name) { var go = GameObject.Find(name); return go ? go.GetComponent<TMP_Text>() : null; }
    private Image FindImage(string name) { var go = GameObject.Find(name); return go ? go.GetComponent<Image>() : null; }

    // ───────── Bind ─────────
    private void TryBind()
    {
        if (subscribed) return;
        if (LobbySelectionState.I == null) return;

        S = LobbySelectionState.I;

        // Host arrows
        if (hostLeftBtn)
        {
            hostLeftBtn.onClick.RemoveAllListeners();
            hostLeftBtn.onClick.AddListener(() => { if (IsLocalHost()) ChangeSelectionServerRpc(0, NetworkManager.ServerClientId); });
        }
        if (hostRightBtn)
        {
            hostRightBtn.onClick.RemoveAllListeners();
            hostRightBtn.onClick.AddListener(() => { if (IsLocalHost()) ChangeSelectionServerRpc(1, NetworkManager.ServerClientId); });
        }

        // Client arrows
        if (clientLeftBtn)
        {
            clientLeftBtn.onClick.RemoveAllListeners();
            clientLeftBtn.onClick.AddListener(() => { if (!IsLocalHost()) ChangeSelectionServerRpc(0, NetworkManager.LocalClientId); });
        }
        if (clientRightBtn)
        {
            clientRightBtn.onClick.RemoveAllListeners();
            clientRightBtn.onClick.AddListener(() => { if (!IsLocalHost()) ChangeSelectionServerRpc(1, NetworkManager.LocalClientId); });
        }

        // Confirm buttons
        if (hostConfirm.button)
        {
            hostConfirm.button.onClick.RemoveAllListeners();
            hostConfirm.button.onClick.AddListener(() => { if (IsLocalHost()) ConfirmServerRpc(NetworkManager.ServerClientId); });
        }
        if (clientConfirm.button)
        {
            clientConfirm.button.onClick.RemoveAllListeners();
            clientConfirm.button.onClick.AddListener(() => { if (!IsLocalHost()) ConfirmServerRpc(NetworkManager.LocalClientId); });
        }

        S.hostReady.OnValueChanged += (_, __) => SafeRefresh();
        S.clientReady.OnValueChanged += (_, __) => SafeRefresh();
        S.hostSlot.OnValueChanged += (_, __) => SafeRefresh();
        S.clientSlot.OnValueChanged += (_, __) => SafeRefresh();

        subscribed = true;
    }

    private void Unsubscribe() => subscribed = false;

    // ───────── Refresh ─────────
    private void SafeRefresh() { try { Refresh(); } catch (System.Exception ex) { Debug.LogError("[CSM] Refresh error: " + ex); } }

    private void Refresh()
    {
        if (S == null) return;

        bool hostPicked = S.hostSlot.Value != 255;
        bool clientPicked = S.clientSlot.Value != 255;
        bool bothPicked = hostPicked && clientPicked;
        bool noConflict = bothPicked ? (S.hostSlot.Value != S.clientSlot.Value) : true;

        if (verboseLog)
        {
            Debug.Log($"[CSM] Refresh -> hostPicked={hostPicked}, clientPicked={clientPicked}, bothPicked={bothPicked}, " +
                      $"noConflict={noConflict}, hostReady={S.hostReady.Value}, clientReady={S.clientReady.Value}");
        }

        // เงื่อนไขขึ้นปุ่ม: “ฝั่งนั้นเลือกแล้ว” และ “ไม่มีชนกัน”
        bool hostCanConfirm = hostPicked && (!clientPicked || noConflict);
        bool clientCanConfirm = clientPicked && (!hostPicked || noConflict);

        bool iAmHost = IsLocalHost();

        // อัปเดต UI: ปุ่มโผล่เฉพาะฝั่งตัวเอง + ต้อง canConfirm + ยังไม่ ready
        UpdateConfirmVisual(hostConfirm, isLocalSide: iAmHost, canConfirm: hostCanConfirm, isReady: S.hostReady.Value);
        UpdateConfirmVisual(clientConfirm, isLocalSide: !iAmHost, canConfirm: clientCanConfirm, isReady: S.clientReady.Value);

        // เริ่มเกมเมื่อทุกคน ready และไม่ชน
        if (IsServer && S.EveryoneReady() && !S.gameStarted.Value)
        {
            if (bothPicked && noConflict)
            {
                if (verboseLog) Debug.Log("[CSM] Everyone ready & noConflict -> StartGame()");
                StartGame();
            }
        }
    }

    // ───────── Confirm UI Logic ─────────
    private void UpdateConfirmVisual(ConfirmUI ui, bool isLocalSide, bool canConfirm, bool isReady)
    {
        if (ui == null) return;

        // ไอคอนติ๊กถูก: โชว์เมื่อ ready (ให้เห็นทั้งสองฝั่ง)
        SetReadyIcon(ui, isReady);

        // ปุ่ม: โชว์เฉพาะฝั่งตัวเอง + ยังไม่ ready + ผ่านเงื่อนไข canConfirm
        bool showButton = isLocalSide && !isReady && canConfirm;

        if (ui.button)
        {
            ui.button.gameObject.SetActive(showButton);
            ui.button.interactable = showButton;
        }

        // Label ของปุ่ม: โชว์เฉพาะตอนปุ่มโผล่ (แก้บัค “Confirm” ไม่ขึ้น)
        if (ui.label)
        {
            ui.label.gameObject.SetActive(showButton);
            if (showButton) ui.label.text = "Confirm";
        }

        // ถ้ามี container, เปิดเมื่ออย่างน้อยมีอย่างใดอย่างหนึ่งโผล่ (ปุ่มหรือไอคอน)
        if (ui.container)
        {
            bool anyVisible = (ui.button && ui.button.gameObject.activeSelf) ||
                              (ui.iconReady && ui.iconReady.gameObject.activeSelf);
            ui.container.SetActive(anyVisible);
        }
    }

    private void SetReadyIcon(ConfirmUI ui, bool visible)
    {
        // กันลืมลาก icon ใน Inspector: fallback หาโดยชื่อมาตรฐาน
        if (!ui.iconReady)
        {
            string fallback = (ui == hostConfirm) ? "IconReady_Host" : "IconReady_Client";
            ui.iconReady = FindImage(fallback);
        }

        if (ui.iconReady)
        {
            ui.iconReady.enabled = visible;                 // เปิด/ปิด Image component
            ui.iconReady.gameObject.SetActive(visible);     // และเปิด/ปิด GameObject
        }
    }

    // ───────── Confirm (toggle ready) ─────────
    [ServerRpc(RequireOwnership = false)]
    private void ConfirmServerRpc(ulong requester)
    {
        bool cur = (requester == NetworkManager.ServerClientId) ? S.hostReady.Value : S.clientReady.Value;
        bool newVal = !cur;
        S.SetReady(requester, newVal);

        if (verboseLog) Debug.Log($"[CSM] ConfirmServerRpc -> requester={requester}, newReady={newVal}");

        // บังคับรีเฟรช UI ทุก client ให้เห็นไอคอนทันที
        ForceRefreshClientRpc();
    }

    // ───────── เลือกตัวละคร ─────────
    [ServerRpc(RequireOwnership = false)]
    private void ChangeSelectionServerRpc(int charIndex, ulong requester)
    {
        if (CharacterCount <= 0) return;
        charIndex = Mathf.Clamp(charIndex, 0, CharacterCount - 1);

        if (requester == NetworkManager.ServerClientId)
            S.hostSlot.Value = (byte)charIndex;
        else
            S.clientSlot.Value = (byte)charIndex;

        S.ClearReadyOf(requester); // เปลี่ยนตัวละคร -> หลุด Ready เสมอ

        // Slide ทุกคลไคลเอนต์
        SlideAllClientsClientRpc((int)S.hostSlot.Value, (int)S.clientSlot.Value);

        // เคลียร์แล้วเลื่อนเสร็จ -> รีเฟรชให้สถานะปุ่ม/ไอคอนตรงกันทุกเครื่อง
        ForceRefreshClientRpc();
    }

    [ClientRpc]
    private void SlideAllClientsClientRpc(int hostIndex, int clientIndex)
    {
        if (verboseLog) Debug.Log($"[CSM] SlideAllClients -> hostIndex={hostIndex}, clientIndex={clientIndex}");
        if (CharacterCount <= 0) return;

        // Host panel
        if (hostIndex >= 0 && hostIndex < CharacterCount)
        {
            var a = characterAnchors[hostIndex];
            if (hostSlotPanel && a.hostAnchor)
            {
                hostSlotPanel.DOKill();
                hostSlotPanel.DOMove(a.hostAnchor.position, moveDuration).SetEase(Ease.OutBack);
            }
        }

        // Client panel
        if (clientIndex >= 0 && clientIndex < CharacterCount)
        {
            var a = characterAnchors[clientIndex];
            if (clientSlotPanel && a.clientAnchor)
            {
                clientSlotPanel.DOKill();
                clientSlotPanel.DOMove(a.clientAnchor.position, moveDuration).SetEase(Ease.OutBack);
            }
        }
    }

    [ClientRpc]
    private void ForceRefreshClientRpc()
    {
        // บังคับรีเฟรช UI ฝั่ง client ทุกเครื่อง
        SafeRefresh();
    }

    private void PositionPanelsToCurrent()
    {
        if (CharacterCount <= 0 || S == null) return;

        int h = S.hostSlot.Value;
        int c = S.clientSlot.Value;

        if (h >= 0 && h < CharacterCount && hostSlotPanel && characterAnchors[h].hostAnchor)
            hostSlotPanel.position = characterAnchors[h].hostAnchor.position;

        if (c >= 0 && c < CharacterCount && clientSlotPanel && characterAnchors[c].clientAnchor)
            clientSlotPanel.position = characterAnchors[c].clientAnchor.position;
    }

    // ───────── Start Game ─────────
    private void StartGame()
    {
        if (!IsServer || S.gameStarted.Value) return;
        S.gameStarted.Value = true;
        if (verboseLog) Debug.Log("[CSM] StartGame -> Loading scene " + gameplaySceneName);
        NetworkManager.Singleton.SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
    }

    // ───────── Join Code ─────────
    private void UpdateJoinCodeUI()
    {
        if (!joinCodeLabel) return;
        string netVal = (LobbySelectionState.I != null) ? LobbySelectionState.I.joinCode.Value.ToString() : "";
        string cacheVal = LobbySelectionState.CachedJoinCode;
        string codeToShow = !string.IsNullOrEmpty(netVal) ? netVal : cacheVal;
        joinCodeLabel.text = string.IsNullOrEmpty(codeToShow) ? "Code: --" : $"Code: {codeToShow}";
    }

    private void CopyJoinCode()
    {
        if (!copyBtn) return;
        string netVal = (LobbySelectionState.I != null) ? LobbySelectionState.I.joinCode.Value.ToString() : "";
        string text = !string.IsNullOrEmpty(netVal) ? netVal : LobbySelectionState.CachedJoinCode;
        if (!string.IsNullOrEmpty(text)) GUIUtility.systemCopyBuffer = text;
    }

    private bool IsLocalHost() => NetworkManager.LocalClientId == NetworkManager.ServerClientId;
}
