namespace ApartmentLife.Runtime
{
    // 住人が今どんな気持ちに見えるかを表す、見た目用の状態です。
    // セーブデータには入れず、イベント発生時の短いリアクションだけに使います。
    public enum EmotionState
    {
        Neutral,
        Happy,
        Angry,
        Sad,
        Worried,
        Surprised,
        Friendly,
        Awkward
    }
}
