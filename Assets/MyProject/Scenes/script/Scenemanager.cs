﻿//내가 사용하는 scenemanager
//여기서 오브젝트들을 받고 


namespace NRKernal.NRExamples.MyArrowProject
{
    using UnityEngine;
    using System.Collections;
    using System;
    using UnityEngine.SceneManagement;
    using UnityEngine.UI;

    public class Scenemanager : MonoBehaviour
    {
        
        public GameObject loadingScene;


        /// <summary>
        /// 전환할 씬이 무거울 경우 비동기화 하여 로딩 시간을 표시하는 슬라이더
        /// 현재 작업한 파일은 둘 다 씬이 가벼워서 금방 넘어간다.
        /// </summary>
        public Slider slider;

        public Canvas compass;

        //public Slider oneSecondbar;

        //private CameraSmoothFollow camerafollower;

        //씬의 이름을 받아오도록 하자.
        [HideInInspector]
        public string scenename;

        //생성할 타입을 고르자.
        [HideInInspector]
        public string scenemode;

        //씬을 몇 번 바꿨고, 혹시 몇 번 이상 바꿨을 때
        //오류가 생기진 않는지 확인용으로
        public int sceneChangeCount = 0;

        public bool compass_setting = false;

        private DateTime datetime;
        

        
        private static Scenemanager m_instance;

        public static Scenemanager Instance
        {
            get
            {
                return m_instance;
            }
        }



        /// <summary>
        /// NRglass가 지원하는 것들을 이용하기 위한 핸들
        /// </summary>
        private ControllerHandEnum m_CurrentDebugHand = ControllerHandEnum.Right;
        public Quaternion smartphonerotation;


        public void ChangeScene(string name)
        {
            StartCoroutine(AsyncLoadScene(name));
        }

        public void Awake()
        {
            //oneSecondbar.value = 0.0f;
            var check = FindObjectsOfType<Scenemanager>();

            compass.gameObject.SetActive(false);

            Debug.Log("check : " + check.Length);

            //씬을 오가면서 여러개 생성되는 경우가 있다.
            //그 경우 이렇게 숫자를 세서 1개 이상이면 삭제한다.
            if(check.Length == 1)
            {
                m_instance = this;
                DontDestroyOnLoad(gameObject);
                datetime = DateTime.Now;
                Debug.Log("GPStour start : " + datetime);
            }
            else if(check.Length > 1)
            {
                Debug.Log("check");
                Destroy(gameObject);
            }

            //Input.gyro.enabled = true;
            //loadingScene.transform.Find("Panel").gameObject.SetActive(false);
            
        }

        private void Update()
        {
            transform.position = NRSessionManager.Instance.CenterCameraAnchor.position;
            transform.rotation = NRSessionManager.Instance.CenterCameraAnchor.rotation;


            if(loadingScene == null)
            {
                return;
            }
            scenename = SceneManager.GetActiveScene().name;
            if (NRInput.GetAvailableControllersCount() < 2)
            {
                m_CurrentDebugHand = NRInput.DomainHand;
            }
            else
            {
                if (NRInput.GetButtonDown(ControllerHandEnum.Right, ControllerButton.TRIGGER))
                {
                    m_CurrentDebugHand = ControllerHandEnum.Right;
                }
                else if (NRInput.GetButtonDown(ControllerHandEnum.Left, ControllerButton.TRIGGER))
                {
                    m_CurrentDebugHand = ControllerHandEnum.Left;
                }
            }

            smartphonerotation = NRInput.GetRotation(m_CurrentDebugHand);
            //OneSecondBarControll();



        }


        /// <summary>
        /// 회전변환을 일으키는 함수, 이 함수가 있어야지 제대로 배치가 되는 듯 하다.
        /// </summary>
        /// <param name="gameobject_base"></param>
        /// <param name="name"></param>
       


        IEnumerator AsyncLoadScene(string name)
        {
            slider.value = 0.0f;
            compass_setting = false;
            loadingScene.SetActive(true);
            

            AsyncOperation operation;
            if (string.Equals(SceneManager.GetActiveScene().name, "SelectManu"))
            {
                operation = SceneManager.LoadSceneAsync("Main Scene");
            }
            else
            {
                sceneChangeCount++;
                operation = SceneManager.LoadSceneAsync("SelectManu");

            }

            scenemode = name;
            Debug.Log("scenemode : " + scenemode);

            //loadingScene.SetActive(true);
            slider.gameObject.SetActive(true);

#if !UNITY_EDITOR
            yield return new WaitUntil(() => GPScontroller.Instance.isConnected);
            Debug.Log("HEEEEEEE");
#endif

            do
            {
                operation.allowSceneActivation = false;
                float progress = Mathf.Clamp(operation.progress, 0, 1) * 10 / 9;
                slider.value = progress;
                Debug.Log("slider value :" + slider.value);
                //Debug.Log(ARLocationProvider.Instance.HasStarted);


                yield return null;
            } while (slider.value < 0.9);

            
            if(scenename == "SelectManu")
            {
                slider.gameObject.SetActive(false);
                loadingScene.SetActive(false);
            }
            else
            {
                slider.gameObject.SetActive(true);
                loadingScene.SetActive(true);
            }

            compass.gameObject.SetActive(true);
            if(scenename == "SelectManu")
            {
                yield return new WaitUntil(() => compass_setting == true);
            }
            

            operation.allowSceneActivation = true;
            compass.gameObject.SetActive(false);
            Debug.Log("operation is done");
            //yield return new WaitUntil(() => Input.compass.enabled);


        }


       
    }

}


