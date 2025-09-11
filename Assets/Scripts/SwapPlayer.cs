using System.Collections;
using Cinemachine;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class SwapPlayer : MonoBehaviour
{
    private enum Player
    {
        Player1,
        Player2
    }

    [SerializeField] private Player currentPlayerControl = Player.Player1;

    [Header("Player 1")]
    [SerializeField] private PlayerInput input_Player1;
    [SerializeField] private CinemachineVirtualCamera camera_Player1;

    [Header("Player 2")]
    [SerializeField] private PlayerInput input_Player2;
    [SerializeField] private CinemachineVirtualCamera camera_Player2;


    [Header("UI")]
    [SerializeField] private TMP_Text textStatusText;

    private void Start()
    {
        StartCoroutine(StartGame());
    }

    IEnumerator StartGame()
    {
        yield return null;
        ControlPlayer(Player.Player1);
    }

    void Update()
    {
        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            if (currentPlayerControl == Player.Player1)
            {
                ControlPlayer(Player.Player2);
            }
            else
            {
                ControlPlayer(Player.Player1);
            }
        }
    }

    void ControlPlayer(Player player)
    {
        if (player == Player.Player1)
        {
            // ปิด P2
            input_Player2.enabled = false;
            camera_Player2.gameObject.SetActive(false);
            // เปิด P1
            input_Player1.enabled = true;
            camera_Player1.gameObject.SetActive(true);

            currentPlayerControl = Player.Player1;
            textStatusText.text = "[Tab] : Player 1";
        }
        else if (player == Player.Player2)
        {
            // ปิด P1
            input_Player1.enabled = false;
            camera_Player1.gameObject.SetActive(false);
            // เปิด P2
            input_Player2.enabled = true;
            camera_Player2.gameObject.SetActive(true);

            currentPlayerControl = Player.Player2;
            textStatusText.text = "[Tab] : Player 2";
        }
    }
}
