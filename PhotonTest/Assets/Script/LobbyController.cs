using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon;
public class LobbyController : PunBehaviour
{
    PhotonView m_photonView;
    string m_gameVer = "1.0";
    string m_playerName;
    bool m_hasNickName = true;
    bool m_isJoined;
    int m_readyCount;
    Dictionary<string, bool> m_playerInfoList = new Dictionary<string, bool>();

    void OnGUI()
    {
        GUI.Label(new Rect(5, 5, 100, 20), PhotonNetwork.connectionStateDetailed.ToString());
        if(!m_isJoined)
        {
            if (m_hasNickName)
            {
                GUI.TextArea(new Rect((Screen.width - 200) / 2, (Screen.height - 50) / 2, 200, 20), m_playerName + "님 반갑습니다");
            }
            else
            {
                m_playerName = GUI.TextField(new Rect((Screen.width - 150) / 2, (Screen.height - 50) / 2, 150, 20), m_playerName);
            }
            if (GUI.Button(new Rect((Screen.width - 150) / 2, (Screen.height - 30) / 2 + 50, 150, 30), "Connect"))
            {
                if (!m_playerName.Equals("이름을 입력하세요") && string.IsNullOrEmpty(m_playerName))
                {
                    var name = PlayerPrefs.GetString("PLAYER_NAME", string.Empty);
                    if (string.IsNullOrEmpty(name))
                    {
                        PlayerPrefs.SetString("PLAYER_NAME", m_playerName);
                    }
                    PhotonNetwork.playerName = m_playerName;
                    PhotonNetwork.ConnectUsingSettings(m_gameVer);
                }
            }
        }
        else
        {
            bool isReady = false;
            GUILayout.BeginArea(new Rect((Screen.width - 400) / 2, (Screen.height - 300) / 2, 400, 300));
            for(int i = 0; i < PhotonNetwork.playerList.Length; i++)
            {
                m_playerInfoList.TryGetValue(PhotonNetwork.playerList[i].NickName, out isReady);
                GUILayout.Label(string.Format("{0}{1}", PhotonNetwork.playerList[i].NickName, PhotonNetwork.playerList[i].IsMasterClient ? " /Host" : isReady ? " /Ready" : string.Empty));
            }
            GUILayout.EndArea();
            if(PhotonNetwork.isMasterClient)
            {
                if(GUI.Button(new Rect((Screen.width + 50 / 2), Screen.height - 100, 150, 30), "START"))
                {
                    if(PhotonNetwork.otherPlayers.Length == m_readyCount)
                    {
                        //호스트가 씬 이름 호출
                        PhotonNetwork.LoadLevel("Game");
                    }
                }
            }
            else
            {
                m_playerInfoList.TryGetValue(m_playerName, out isReady);
                if(GUI.Button(new Rect((Screen.width + 50) / 2, Screen.height - 100, 150, 30), isReady ? "Cancle" : "Ready"))
                {
                    m_playerInfoList[m_playerName] = !isReady;
                    PhotonView.Get(this).RPC("SendReady", PhotonTargets.OthersBuffered, m_playerName, !isReady);
                }
            }
        }
        
    }



    // Start is called before the first frame update
    void Start()
    {
        m_readyCount = 0;
        m_isJoined = false;
        m_photonView = gameObject.AddComponent<PhotonView>();
        m_photonView.viewID = 1;
        //호스트따라 참가자들도 다같이 씬 호출
        PhotonNetwork.automaticallySyncScene = true;
        
        m_playerName = PlayerPrefs.GetString("PLAYER_NAME", string.Empty);
        if(!string.IsNullOrEmpty(m_playerName))
        {
            m_hasNickName = true;
        }
        else
        {
            m_hasNickName = false;
            m_playerName = "이름을 입력하세요";
        }
    }
    #region Photon CallBack Methods
    public override void OnJoinedLobby()
    {
        Debug.Log("로비접속");
        PhotonNetwork.JoinRandomRoom();
    }
    public override void OnPhotonRandomJoinFailed(object[] codeAndMsg)
    {
        Debug.Log("랜덤 룸 접속 실패");
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = 4 }, null);
    }
    public override void OnJoinedRoom()
    {
        Debug.Log("방 입장");
        m_isJoined = true;
        PhotonView.Get(this).RPC("SendJoinedPlayer", PhotonTargets.OthersBuffered, m_playerName);
    }
    public override void OnPhotonPlayerConnected(PhotonPlayer newPlayer)
    {
        Debug.Log("플레이어 입장" + newPlayer.NickName);
        // othersbuffered -> 버퍼에 쌓아서 통신이 조금 지연되더라도 보냄
        PhotonView.Get(this).RPC("SendJoinedPlayer", PhotonTargets.OthersBuffered, m_playerName);
    }
    #endregion
    [PunRPC]
    void SendJoinedPlayer(string name)
    {
        if(m_playerInfoList.ContainsKey(name))
        {
            m_playerInfoList.Add(name, false);
            Debug.Log(name + "추가");
        }
    }
    [PunRPC]
    void SendReady(string name, bool isReady)
    {
        Debug.Log(name + " / " + isReady);
        m_playerInfoList[name] = isReady;
        if (isReady) m_readyCount++;
        else m_readyCount--;
    }
}
