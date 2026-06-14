using System;

namespace ApartmentLife.Data
{
    // 2人の住人の関係値を保存するためのデータです。
    [Serializable]
    public class RelationshipData
    {
        public string residentAId;
        public string residentBId;
        public int value;

        public RelationshipData(string residentAId, string residentBId, int value)
        {
            this.residentAId = residentAId;
            this.residentBId = residentBId;
            this.value = value;
        }
    }
}
