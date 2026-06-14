using System;
using System.Collections.Generic;

namespace ApartmentLife.Data
{
    // JsonUtilityはトップレベルのListを直接保存しづらいので、保存用の入れ物を作ります。
    [Serializable]
    public class GameSaveData
    {
        public int day;
        public List<ResidentData> residents = new List<ResidentData>();
        public List<RelationshipData> relationships = new List<RelationshipData>();
        public List<string> eventLogs = new List<string>();
    }
}
