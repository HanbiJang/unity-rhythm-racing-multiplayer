using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{
    public class ResultUIController : MonoBehaviour
    {
        [SerializeField] Transform rankingRoot; //  contents 
        [SerializeField] GameObject rankingItemPrefab; // 랭킹 점수 표시 프리팹 

        void OnEnable()
        {
            Refresh();
        }
        /// <summary>
        /// gamestate 의 점수 리스트들을 순서대로 정렬
        /// </summary>
        public void Refresh()
        {
            foreach (Transform child in rankingRoot) Destroy(child.gameObject);

            var list = GameState.Instance.ScoreLIst; // List<KeyValuePair<ulong, ulong>>
            if (list == null) return;

            // 점수 내림차순 정렬 ( 점수 높은 인간이 상위에 )
            var sorted = new List<KeyValuePair<ulong, ulong>>(list);
            sorted.Sort((a, b) => b.Value.CompareTo(a.Value));

            foreach (var kv in sorted)
            {
                var go = Instantiate(rankingItemPrefab, rankingRoot);
                var labels = go.GetComponentsInChildren<UnityEngine.UI.Text>();
                string nickname = null;
                if (GameState.Instance.UserNicknames != null)
                {
                    GameState.Instance.UserNicknames.TryGetValue(kv.Key, out nickname);
                }
                labels[0].text = $"User: {(string.IsNullOrEmpty(nickname) ? kv.Key.ToString() : nickname)}";
                labels[1].text = $"Score: {kv.Value}";
            }
        }

        // 게임 종료 후 로비로 가는 버튼에 연결
        public void OnClickBackToLobby()
        {
            ResultFlow.BackToLobby();
        }
    }

}
