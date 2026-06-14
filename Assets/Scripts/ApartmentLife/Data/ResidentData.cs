using System;

namespace ApartmentLife.Data
{
    // JsonUtilityで保存できるように、住人の情報はSerializableなクラスにまとめます。
    [Serializable]
    public class ResidentData
    {
        public string id;
        public string name;
        public string personality;
        public string hobby;
        public string catchphrase;
        public int mood;
        public int energy;

        public ResidentData(string id, string name, string personality, string hobby, string catchphrase, int mood, int energy)
        {
            this.id = id;
            this.name = name;
            this.personality = personality;
            this.hobby = hobby;
            this.catchphrase = catchphrase;
            this.mood = mood;
            this.energy = energy;
        }
    }
}
