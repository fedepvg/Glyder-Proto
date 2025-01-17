﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BirdController : MonoBehaviour
{
    #region PublicVariables
    public float Speed;
    public float HorizontalSpeed;
    public float BaseGravity;
    public Vector3 MinRotation;
    public Vector3 MaxRotation;
    public float RotationSpeed;
    public float DownRotationCoefficient;
    public float MiddleRotationCoefficient;
    public float UpRotationCoefficient;
    public float JumpSpeed;
    public float MaxEnergy;
    public float MaxSpeedMultiplier;
    public float MinSpeedMultiplier;
    public Animator AnimationController;
    public AnimationCurve JumpCurve;
    public LayerMask LevelRaycastLayer;
    public LayerMask AltitudeRaycastLayer;
    public GameObject BlobShadow;
    public SceneLoader SceneManagement;
    public float LevelDistanceLeft;
    public float Energy;
    public float JumpEnergyLoss;
    public float EnergyLossCoefficient;
    public PlayerControls PlayerInput;
    public delegate void OnGameOver(bool won);
    public static OnGameOver GameOverAction;
    public delegate void OnEndLevel();
    public static OnEndLevel EndLevelAction;
    public delegate void OnPlayerMoving(float speedPorc);
    public static OnPlayerMoving OnPlayerMovingAction;
    public float TimeToEndLevel;
    public float TimeToGameOverScreen;
    public float OffLimitsRotationMultiplier;
    public GameObject DeathParticlePrefab;
    public GameObject WaterDeathParticlePrefab;
    public AK.Wwise.RTPC EnergyRTPCParameter;
    public AK.Wwise.RTPC SpeedRTPCParameter;
    public AK.Wwise.RTPC DistanceRTPCParameter;
    public AK.Wwise.RTPC AltitudeRTPCParameter;
    public Transform StartTransform;
    public Transform FinishTransform;
    public Image EnergyFilter;
    #endregion

    #region PrivateVariables
    Rigidbody Rigi;
    bool IsJumping;
    Vector3 FrameRotation;
    Vector3 Rotation;
    Quaternion DestRotation;
    float SpeedMultiplier;
    float Gravity;
    float JumpTimer;
    float JumpGravity;
    const float LevelRayDistance = 3000f;
    float RotationCoefficient;
    public bool OffLeftLimit = false;
    public bool OffRightLimit = false;
    bool EndedLevel = false;
    const float MaxSpeed = 70f;
    const float MaxAltitude = 50f;
    float PlayerAltitude;
    float AltitudePorc;
    float LevelDistance;
    float LevelDistancePorc;
    bool IsAlive = true;
    #endregion

    void Start()
    {
        Rigi = GetComponent<Rigidbody>();

        PlayerInput = GameManager.Instance.GameInput;
        PlayerInput.Enable();

        IsJumping = false;

        Rotation.x = 0f;
        Rotation.z = 0f;

        DestRotation = Quaternion.identity;

        SpeedMultiplier = 0.8f;
        Energy = MaxEnergy;

        if(FinishTransform)
            LevelDistance = FinishTransform.position.z - StartTransform.position.z;

        transform.position=StartTransform.position;

        OrbBehaviour.OnOrbPickup = AddEnergy;

        AkSoundEngine.PostEvent("Pajaro_Voz", gameObject);
        GameManager.Instance.StartCountingTime();
    }
    
    void Update()
    {
        #region Jump
        if (PlayerInput.Gameplay.Jump.triggered && !IsJumping && Energy > EnergyLossCoefficient)
        {
            IsJumping = true;
            AnimationController.SetTrigger("Fly");
            AkSoundEngine.PostEvent("Pajaro_Aletea", gameObject);
        }

        if(PlayerInput.Gameplay.Horizontal.enabled)
            FrameRotation.z = -PlayerInput.Gameplay.Horizontal.ReadValue<float>();
        if (PlayerInput.Gameplay.Vertical.enabled)
        {
            if (GameManager.Instance.InvertedY)
                FrameRotation.x = PlayerInput.Gameplay.Vertical.ReadValue<float>();
            else
                FrameRotation.x = -PlayerInput.Gameplay.Vertical.ReadValue<float>();
        }

        if (IsJumping)
        {
            JumpGravity = JumpSpeed * JumpCurve.Evaluate(JumpTimer);
            JumpTimer = Mathf.Clamp01(JumpTimer += Time.deltaTime);

            if (JumpTimer >= 0.9f)
                IsJumping = false;

            JumpEnergyLoss = JumpGravity / 2;
        }
        else
        {
            JumpTimer = 0;
            JumpGravity = 0f;
            JumpEnergyLoss = 1f;
        }
        #endregion

        #region Rotation
        if (OffLeftLimit)
        {
            FrameRotation.z -= OffLimitsRotationMultiplier * Time.deltaTime;
        }
        else if (OffRightLimit)
        {
            FrameRotation.z += OffLimitsRotationMultiplier * Time.deltaTime;
        }

        if (EndedLevel)
            FrameRotation.x = -2;

        Rotation.x += FrameRotation.x * RotationSpeed * Time.deltaTime;
        Rotation.z += FrameRotation.z * RotationSpeed * Time.deltaTime;

        Rotation.x = Mathf.Clamp(Rotation.x, MinRotation.x, MaxRotation.x);
        Rotation.z = Mathf.Clamp(Rotation.z, MinRotation.z, MaxRotation.z);

        if (Rotation.z >= MaxRotation.z && OffRightLimit)
        {
            PlayerInput.Gameplay.Horizontal.Enable();
            //OffRightLimit = false;
        }
        else if (Rotation.z <= MinRotation.z && OffLeftLimit)
        {
            PlayerInput.Gameplay.Horizontal.Enable();
            //OffLeftLimit = false;
        }

        DestRotation = Quaternion.Euler(Rotation.x, 0f, Rotation.z);
        #endregion

        #region MovementCalculations
        if (Rotation.x < 4f && Energy > 0)
        {
            RotationCoefficient = DownRotationCoefficient;
            if (Rotation.x > 0)
                RotationCoefficient = MiddleRotationCoefficient;
            AnimationController.SetBool("GoingDown", false);
        }
        else
        {
            RotationCoefficient = UpRotationCoefficient;
            if (Rotation.x >= 4f)
                AnimationController.SetBool("GoingDown", true);
            else
                AnimationController.SetBool("GoingDown", false);
        }

        SpeedMultiplier += (SpeedMultiplier * RotationCoefficient * Rotation.x) * Time.deltaTime;
        SpeedMultiplier = Mathf.Clamp(SpeedMultiplier, MinSpeedMultiplier, MaxSpeedMultiplier);

        Gravity = BaseGravity / SpeedMultiplier + JumpGravity;
        #endregion

        #region EnergyCalculation
        Energy -= EnergyLossCoefficient  * JumpEnergyLoss * Time.deltaTime;
        Energy = Mathf.Clamp(Energy, 0, MaxEnergy);
        EnergyFilter.color = new Vector4(0.1f, 0, 0, 0.9f - Energy / 100);
        #endregion

        #region LevelDistance
        RaycastHit hit;
        string layerHitted;
        if (Physics.Raycast(transform.position, Vector3.forward, out hit, LevelRayDistance, LevelRaycastLayer))
        {
            layerHitted = LayerMask.LayerToName(hit.transform.gameObject.layer);

            if (layerHitted == "Finish")
            {
                LevelDistanceLeft = hit.distance;
                LevelDistancePorc = LevelDistanceLeft / LevelDistance;

            }
        }
        #endregion

        #region FOVModify
        float speedPorc = Rigi.velocity.magnitude / MaxSpeed;
        if (OnPlayerMovingAction != null)
            OnPlayerMovingAction(speedPorc);
        #endregion

        #region Altitude
        if (Physics.Raycast(transform.position, Vector3.down, out hit, LevelRayDistance, AltitudeRaycastLayer))
        {
            layerHitted = LayerMask.LayerToName(hit.transform.gameObject.layer);

            if (layerHitted == "Floor")
            {
                PlayerAltitude = hit.distance;
                AltitudePorc = PlayerAltitude / MaxAltitude;
            }
        }
        #endregion

        #region RTPC
        EnergyRTPCParameter.SetGlobalValue(Energy / 100);
        SpeedRTPCParameter.SetGlobalValue(speedPorc);
        DistanceRTPCParameter.SetGlobalValue(LevelDistancePorc);
        AltitudeRTPCParameter.SetGlobalValue(AltitudePorc);
        #endregion

        UpdateBlobShadowPosition();
    }

    void FixedUpdate()
    {
        if (IsAlive)
        {
            Rigi.velocity = transform.forward * Speed * SpeedMultiplier;
            Rigi.velocity += new Vector3(0f, Gravity, 0f);
            Rigi.velocity += Vector3.right * -Rotation.z * HorizontalSpeed * Time.fixedDeltaTime;
            Rigi.MoveRotation(DestRotation);
        }
    }

    void UpdateBlobShadowPosition()
    {
        BlobShadow.transform.position = transform.position;
    }

    void AddEnergy(int energy)
    {
        Energy += energy;
        if (Energy >= MaxEnergy)
            Energy = MaxEnergy;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Wall" || collision.gameObject.tag == "Obstacle" || collision.gameObject.tag == "Floor")
        {
            
                if (collision.gameObject.tag != "Floor")
                {
                    Instantiate(DeathParticlePrefab, transform.position, Quaternion.identity);
                    AkSoundEngine.PostEvent("Colision_Madera", gameObject);
                }
                else
                {
                    Instantiate(WaterDeathParticlePrefab, collision.contacts[0].point, Quaternion.identity);
                    AkSoundEngine.PostEvent("Colision_Agua", gameObject);
                }
            if (IsAlive)
                StartCoroutine(Die(TimeToGameOverScreen));
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "CityLimit")
        {
            if (Rigi.velocity.x > 0)
            {
                OffRightLimit = true;
            }
            else
            {
                OffLeftLimit = true;
            }
            PlayerInput.Gameplay.Horizontal.Disable();
        }

        if (other.tag == "Finish" && IsAlive)
        {
            StartCoroutine(EndLevel(TimeToEndLevel));
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if(other.tag=="CityLimit")
        {
            OffRightLimit = false;
            OffLeftLimit = false;
        }
    }

    IEnumerator Die(float t)
    {
        AkSoundEngine.PostEvent("Perder", gameObject);
        AnimationController.SetTrigger("Death");
        if (EndLevelAction != null)
            EndLevelAction();
        PlayerInput.Gameplay.Disable();
        Rigi.velocity /= 2;
        Rigi.useGravity = true;
        Rigi.mass = 100;
        Rigi.freezeRotation = true;
        IsAlive = false;

        yield return new WaitForSeconds(t);

        if (GameOverAction != null)
            GameOverAction(false);
    }

    IEnumerator EndLevel(float t)
    {
        AkSoundEngine.PostEvent("Ganar", gameObject);
        PlayerInput.Gameplay.Disable();
        EndedLevel = true;
        BaseGravity = 0;
        if (EndLevelAction != null)
            EndLevelAction();

        yield return new WaitForSeconds(t);

        EnergyRTPCParameter.SetGlobalValue(1);
        if (GameOverAction!=null)
            GameOverAction(true);
        Destroy(this);
    }

    private void OnDestroy()
    {
        EnergyRTPCParameter.SetGlobalValue(1);
    }
}
