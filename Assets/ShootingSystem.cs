using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class ShootingSystem : MonoBehaviour
{

    [SerializeField] ParticleSystem inkParticle;
    [SerializeField] ParticleSystem extraParticle;
    [SerializeField] Transform[] ikHandlers;
    [SerializeField] Transform splatGun;
    [SerializeField] Transform splatGunNozzle;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

        GetComponent<MovementInput>().blockRotationPlayer= Input.GetMouseButton(0);
        if (Input.GetMouseButton(0))
        {
            foreach (Transform handler in ikHandlers)
            {
                if (!DOTween.IsTweening(handler))
                {
                    handler.DOComplete();
                    handler.DOPunchPosition(-transform.forward / 13, .15f, 10, 1);
                }
            }

            if (!DOTween.IsTweening(splatGunNozzle))
            {
                splatGunNozzle.DOComplete();
                splatGunNozzle.DOPunchScale(new Vector3(0,1,1) / 3, .15f, 10, 1);
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
    }
}
