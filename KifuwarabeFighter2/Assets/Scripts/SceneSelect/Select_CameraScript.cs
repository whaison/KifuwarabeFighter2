﻿using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

namespace SceneSelect
{
    public class Select_CameraScript : MonoBehaviour
    {
        Animator[] player_to_animator;
        /// <summary>
        /// computer character selecting minimum time; コンピューターがキャラクターセレクトしている最低時間。
        /// </summary>
        public const int READY_TIME_LENGTH = 180;
        /// <summary>
        /// computer character selecting minimum time counter; コンピューターがキャラクターセレクトしている最低時間カウンター。
        /// </summary>
        int readyingTime; public int ReadyingTime { get { return readyingTime; } set { readyingTime = value; } }

        void Start()
        {
            player_to_animator = new[] { GameObject.Find(SceneCommon.PlayerAndGameobject_to_path[(int)PlayerIndex.Player1,(int)GameobjectIndex.Player]).GetComponent<Animator>(), GameObject.Find(SceneCommon.PlayerAndGameobject_to_path[(int)PlayerIndex.Player2, (int)GameobjectIndex.Player]).GetComponent<Animator>() };

            // このシーンのデータベースを用意するぜ☆（＾▽＾）
            AstateDatabase.Instance.InsertAllStates();
        }

        // Update is called once per frame
        void Update()
        {
            ReadyingTime++;

            // 現在のアニメーター・ステートに紐づいたデータ
            AstateRecordable astateRecord0 = AstateDatabase.Instance.GetCurrentAstateRecord(player_to_animator[(int)PlayerIndex.Player1]);
            AstateRecordable astateRecord1 = AstateDatabase.Instance.GetCurrentAstateRecord(player_to_animator[(int)PlayerIndex.Player2]);
            if (
                AstateDatabase.Instance.index_to_record[(int)AstateIndex.Ready].Name == astateRecord0.Name
                &&
                AstateDatabase.Instance.index_to_record[(int)AstateIndex.Ready].Name == astateRecord1.Name
                )
            {
                // １プレイヤー、２プレイヤー　ともに Ready ステートなら。
                player_to_animator[(int)PlayerIndex.Player1].SetTrigger(SceneCommon.TRIGGER_TIMEOVER);
                player_to_animator[(int)PlayerIndex.Player2].SetTrigger(SceneCommon.TRIGGER_TIMEOVER);
                SceneCommon.TransitionTime = 1;
            }

            if (0 < SceneCommon.TransitionTime)
            {
                SceneCommon.TransitionTime++;

                if (5 == SceneCommon.TransitionTime)
                {
                    SceneManager.LoadScene(CommonScript.Scene_to_name[(int)SceneIndex.Main]);
                }
            }
        }
    }
}
