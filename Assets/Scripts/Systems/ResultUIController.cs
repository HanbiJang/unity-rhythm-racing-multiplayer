using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

            for (int i = 0; i < sorted.Count; i++)
            {
                var kv = sorted[i];
                var go = Instantiate(rankingItemPrefab, rankingRoot);
                var labels = go.GetComponentsInChildren<UnityEngine.UI.Text>();
                string nickname = null;
                if (GameState.Instance.UserNicknames != null)
                {
                    GameState.Instance.UserNicknames.TryGetValue(kv.Key, out nickname);
                }
                labels[0].text = $"User: {(string.IsNullOrEmpty(nickname) ? kv.Key.ToString() : nickname)}";
                labels[1].text = $"Score: {kv.Value}";

                // 첫 번째 랭킹(최고 점수)의 경우 "1st" 이미지 활성화
                if (i == 0)
                {
                    // "1st" 이름을 가진 GameObject를 찾아서 활성화
                    Transform firstImageTransform = go.transform.Find("1st");
                    if (firstImageTransform == null)
                    {
                        // 다른 가능한 이름들 시도
                        firstImageTransform = go.transform.Find("1st Image");
                        if (firstImageTransform == null)
                        {
                            firstImageTransform = go.transform.Find("1stImg");
                        }
                    }

                    if (firstImageTransform != null)
                    {
                        firstImageTransform.gameObject.SetActive(true);
                        Debug.Log($"[ResultUIController] Activated 1st image for top player: {(string.IsNullOrEmpty(nickname) ? kv.Key.ToString() : nickname)}");
                    }
                    else
                    {
                        Debug.LogWarning($"[ResultUIController] Could not find '1st' image in RankingItem prefab. Please ensure the GameObject is named '1st', '1st Image', or '1stImg'.");
                    }
                }
            }
        }

        // 게임 종료 후 로비로 가는 버튼에 연결
        public void OnClickBackToLobby()
        {
            ResultFlow.BackToLobby();
        }
}
