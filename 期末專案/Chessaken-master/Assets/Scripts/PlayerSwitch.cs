using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSwitch : MonoBehaviour
{
    // Start is called before the first frame update
    public bool isThird;
    public GameObject pausePage,pauseBtn;
    public GameObject thirdCam, firstCam;
    public GameObject Dir;
    public float hrotateRate, vrotateRate;
    public float curV,maxV;
    public float moveSpeed, runSpeed;
    public KeyCode runKey;
    private Vector3 pre_mousePos;
    private Vector3 rot_offset;
    float targetYaw;
    float targetPitch;

    float currentYaw;
    float currentPitch;

    float yawVel;
    float pitchVel;

    public float smoothTime = 0.08f;
    void Start()
    {
        Time.timeScale = 1;
        Vector3 e = thirdCam.transform.eulerAngles;

        // Unity 的 Euler 是 0~360，要轉成 -180~180
        float pitch = NormalizeAngle(e.x);

        currentPitch = targetPitch = pitch;
        currentYaw = targetYaw = e.y;

        // 🔑 關鍵：curV 與實際角度同步
        curV = pitch;

        pre_mousePos = Input.mousePosition;
    }
    float NormalizeAngle(float angle)
    {
        if (angle > 180f) angle -= 360f;
        return angle;
    }
    // Update is called once per frame
    void Update()
    {
        if (pre_mousePos == Vector3.zero)
        {
            pre_mousePos = Input.mousePosition;
        }
        else
        {
            rot_offset = Input.mousePosition - pre_mousePos;
            rot_offset.y = -rot_offset.y;
            pre_mousePos = Input.mousePosition;
        }
        if (isThird)
        {
            Dir.transform.eulerAngles = new Vector3(0, thirdCam.transform.eulerAngles.y, 0);
            Vector2 moveDir = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            if(Input.GetKey(runKey)) thirdCam.transform.position += Dir.transform.TransformDirection(new Vector3(moveDir.x, 0, moveDir.y)) * runSpeed * Time.deltaTime;
            else thirdCam.transform.position += Dir.transform.TransformDirection(new Vector3(moveDir.x, 0, moveDir.y)) * moveSpeed * Time.deltaTime;
            if (pre_mousePos == Vector3.zero)
            {
                pre_mousePos = Input.mousePosition;
            }
            else
            {
                // ===== 垂直（保留你原本的限制）=====
                float vDelta = rot_offset.y * vrotateRate * Time.deltaTime;

                if ((curV < maxV && vDelta > 0) ||
                    (curV > -maxV && vDelta < 0))
                {
                    curV += vDelta;
                    targetPitch += vDelta;
                }

                // ===== 水平（不限制）=====
                float hDelta = rot_offset.x * hrotateRate * Time.deltaTime;
                targetYaw += hDelta;

                // ===== 平滑追上 =====
                currentPitch = Mathf.SmoothDampAngle(
                    currentPitch,
                    targetPitch,
                    ref pitchVel,
                    smoothTime
                );

                currentYaw = Mathf.SmoothDampAngle(
                    currentYaw,
                    targetYaw,
                    ref yawVel,
                    smoothTime
                );

                // ===== 套用旋轉 =====
                thirdCam.transform.rotation = Quaternion.Euler(
                    currentPitch,
                    currentYaw,
                    0f
                );
            }
        }
    }
    public void Switch()
    {
        if (isThird)
        {
            isThird = false;
            thirdCam.SetActive(false);
            firstCam.SetActive(true);         
        }
        else
        {
            isThird = true;
            thirdCam.SetActive(true);
            firstCam.SetActive(false);
        }
    }
    public void Pause(bool flag)
    {
        if (flag)
        {
            pausePage.SetActive(true);
            pauseBtn.SetActive(false);
            Time.timeScale = 0;
        }
        else
        {
            pausePage.SetActive(false);
            pauseBtn.SetActive(true);
            Time.timeScale = 1;
        }
    }
    public void BackToTitle()
    {
        SceneManager.LoadScene(0);
    }
}
