using System;

namespace ApartmentLife.Data
{
    // 主人公用の保存データです。色は選択肢名で保存し、表示時にColorへ変換します。
    [Serializable]
    public class PlayerData
    {
        public string id;
        public string name;
        public string personality;
        public string hobby;
        public string catchphrase;
        public int mood;
        public int energy;
        public string bodyColor;
        public string clothesColor;
        public bool isPlayer;

        public PlayerData(string id, string name, string personality, string hobby, string catchphrase, int mood, int energy, string bodyColor, string clothesColor, bool isPlayer)
        {
            this.id = id;
            this.name = name;
            this.personality = personality;
            this.hobby = hobby;
            this.catchphrase = catchphrase;
            this.mood = mood;
            this.energy = energy;
            this.bodyColor = bodyColor;
            this.clothesColor = clothesColor;
            this.isPlayer = isPlayer;
        }
    }
}
