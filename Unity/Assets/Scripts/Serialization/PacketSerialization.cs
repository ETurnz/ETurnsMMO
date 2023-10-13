using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PacketSerialization : MonoBehaviour
{
    [Serializable]
    public class BaseMessage
    {
        public string clientID;
        public string messageType;

        public BaseMessage(string cID, string mType)
        {
            clientID = cID;
            messageType = mType;
        }
        public BaseMessage()
        {
            clientID = null;
            messageType = null;
        }
    }

    [Serializable]
    public class LogInAttempt
    {
        public string clientID;
        public string messageType;
        public string email;
        public string hashedPassword;

        public LogInAttempt(string cID, string mType, string e, string hPW)
        {
            clientID = cID;
            messageType = mType;
            email = e;
            hashedPassword = hPW;
        }
    }

    [Serializable]
    public class CharacterDataResponse
    {
        public string clientID;
        public string messageType;
        public CharacterData[] characters;
    }

    [Serializable]
    public class CharacterData
    {
        public string name;
        public string classType;
        public int combatLevel;
        public int zoneID;
        public float posX;
        public float posY;
        public float posZ;
        public float rotX;
        public float rotY;
        public float rotZ;
        public float rotW;
        public bool IsRunning;
        public bool IsRolling;
        public bool IsStrafingLeft;
        public bool IsStrafingRight;
    }

    [Serializable]
    public class PartyUpdate
    {
        public string clientID;
        public string messageType;
        public List<PartyMember> partyMembers;

        public PartyUpdate()
        {
            partyMembers = new List<PartyMember>();
        }
    }

    [Serializable]
    public class PartyMember
    {
        public string playerName;
        public string playerHealth;
        public string playerEnergy;
        public bool isLeader;
    }

    public class BasicText
    {
        public string clientID;
        public string messageType;
        public string text;

        public BasicText(string cID, string mType, string t)
        {
            clientID = cID;
            messageType = mType;
            text = t;
        }
    }

    public class LoadCharacter
    {
        public string clientID;
        public string messageType;

        public CharacterData characterData;
    }

    public class PlayerUpdate
    {
        public string clientID;
        public string messageType;
        public string name;
        public float posX;
        public float posY;
        public float posZ;
        public float rotX;
        public float rotY;
        public float rotZ;
        public float rotW;
        public bool IsRunning;
        public bool IsRolling;
        public bool IsStrafingLeft;
        public bool IsStrafingRight;

        public PlayerUpdate(string cID, string mType, string n, float pX, float pY, float pZ, float rX, float rY, float rZ, float rW, bool isRun, bool isRoll, bool isStrafeLeft, bool isStrafeRight)
        {
            clientID = cID;
            messageType = mType;
            name = n;
            posX = pX;
            posY = pY;
            posZ = pZ;
            rotX = rX;
            rotY = rY;
            rotZ = rZ;
            rotW = rW;
            IsRunning = isRun;
            IsRolling = isRoll;
            IsStrafingLeft = isStrafeLeft;
            IsStrafingRight = isStrafeRight;
        }
    }

    public class PlayerInvite
    {
        public string clientID;
        public string messageType;
        public string inviteName;

        public PlayerInvite(string cID, string mType, string iN)
        {
            clientID = cID;
            messageType = mType;
            inviteName = iN;
        }
    }
}
