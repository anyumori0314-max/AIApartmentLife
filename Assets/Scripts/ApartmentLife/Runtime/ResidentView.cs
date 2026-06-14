using ApartmentLife.Data;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ApartmentLife.Runtime
{
    // 3D上の住人オブジェクトに付けるクリック受付用コンポーネントです。
    public class ResidentView : MonoBehaviour, IPointerClickHandler
    {
        private ApartmentLifeGame game;
        private ResidentData resident;

        public ResidentData Resident => resident;

        public void Initialize(ApartmentLifeGame owner, ResidentData data)
        {
            game = owner;
            resident = data;
            gameObject.name = $"Resident_{data.name}";
        }

        private void OnMouseDown()
        {
            // コライダー付きのオブジェクトをクリックするとUnityがこの関数を呼びます。
            SelectThisResident();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // Input SystemのUI入力経由でもクリックできるようにします。
            SelectThisResident();
        }

        private void SelectThisResident()
        {
            if (game != null && resident != null)
            {
                game.SelectResident(resident);
            }
        }
    }
}
