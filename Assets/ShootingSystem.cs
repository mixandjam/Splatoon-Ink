using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Cinemachine;

public class ShootingSystem : MonoBehaviour
{

    [SerializeField] ParticleSystem inkParticle;
    [SerializeField] ParticleSystem extraParticle;
    [SerializeField] Transform parentController;
    [SerializeField] Transform splatGunNozzle;
    [SerializeField] CinemachineFreeLook freeLookCamera;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 angle = parentController.localEulerAngles;
        GetComponent<MovementInput>().blockRotationPlayer = Input.GetMouseButton(0);
        bool pressing = GetComponent<MovementInput>().blockRotationPlayer;
        if (Input.GetMouseButton(0))
        {
            if (!DOTween.IsTweening(parentController))
            {
                parentController.DOComplete();
                Vector3 forward = -parentController.forward;
                Vector3 localPos = parentController.localPosition;
                parentController.DOLocalMove(localPos - new Vector3(0, 0, .1f), .03f)
                    .OnComplete(() => parentController.DOLocalMove(localPos,.1f).SetEase(Ease.OutSine));
            }

            if (!DOTween.IsTweening(splatGunNozzle))
            {
                splatGunNozzle.DOComplete();
                splatGunNozzle.DOPunchScale(new Vector3(0,1,1) / 1.5f, .15f, 10, 1);
            }

            GetComponent<MovementInput>().RotateToCamera(transform);
        }
        if (Input.GetMouseButtonDown(0))
        {
            inkParticle.Play();
            extraParticle.Play();
        }
        else if (Input.GetMouseButtonUp(0))
        {
            inkParticle.Stop();
            extraParticle.Stop();
        }

        parentController.localEulerAngles 
            = new Vector3(Mathf.LerpAngle(parentController.localEulerAngles.x, pressing ? RemapCamera(freeLookCamera.m_YAxis.Value, 0, 1, -25, 25) : 0,.3f), angle.y, angle.z);
    }


    float RemapCamera(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }
}
