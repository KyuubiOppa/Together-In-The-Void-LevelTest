using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
public class NetworkLobby : NetworkBehaviour
{
    [Header("UI Lobby Elements")]
    public GameObject lobbyUI;
    public Button hostButton;
    public Button joinButton;
    public TMP_InputField roomCodeInput;
    [Header("UI Character Selection Elements")]
    public GameObject characterSelectionUI;
    public TMP_Text roomCodeText;
    public Button copyRoomCodeBtn;
    public Button selectCharacterA;
    public Button selectCharacterB;
    public Button startGameButton;

    [Header("Characters")]
    public GameObject playerA_Prefab;
    public Transform playerA_Spawnpoint;
    public GameObject playerB_Prefab;
    public Transform playerB_Spawnpoint;

    void Start()
    {
        
    }
}
