using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace TriangleFactory
{
	public class Player : MonoBehaviour
	{
        public static event EventHandler OnWeaponEquipped;

        [SerializeField] LayerMask _interactionMask;
        [SerializeField] LayerMask weaponLayerMask;
        [SerializeField] LayerMask targetLayerMask;
		[SerializeField] Camera _camera;
		[SerializeField] Transform _weaponLocation;
        [SerializeField] GameObject shotParticle;
        [SerializeField] GameObject impactDecal;
 
        [SerializeField] float movementSpeed = 5f;

		Weapon _weapon;
        InputManager inputManagerScript;
        ScoreManager scoreManagerScript;
        AudioManager audioManagerScript;
        UIManager uiManagerScript;

        RaycastHit hitInfo;

        bool equippedWeapon;

        float timeSinceLastShot = Mathf.Infinity;
        float reloadTime = .5f;

        int pointsOnMiss = -100;
        int pointsOnHit = 1000;
        int pointsOnHitBomb = -2000;

        void Awake()
        {
            inputManagerScript = FindObjectOfType<InputManager>();
            scoreManagerScript = FindObjectOfType<ScoreManager>();
            audioManagerScript = FindObjectOfType<AudioManager>();
            uiManagerScript = FindObjectOfType<UIManager>();
        }

        void Update()
        {
            timeSinceLastShot += Time.deltaTime; 
            UpdatePlayerPositionAndRotation();
            TryEquipWeapon();
            TryShoot();
        }

        void TryEquipWeapon()
        {
            if (inputManagerScript.Interact() == false) return;
            if (Physics.Raycast(GetRay(), out RaycastHit hit, float.MaxValue, weaponLayerMask) == false) return;

            var weapon = hit.collider.GetComponentInParent<Weapon>();
            EquipWeapon(weapon);
        }

        void EquipWeapon(Weapon weapon)
        {
            if (_weapon != null) return;
            _weapon = weapon;
            _weapon.transform.SetParent(_weaponLocation);
            _weapon.transform.localPosition = Vector3.zero;
            _weapon.transform.localRotation = Quaternion.identity;
            equippedWeapon = true;
            OnWeaponEquipped?.Invoke(this, EventArgs.Empty);
            FindObjectOfType<TargetManager>().DisableTargets();
        }

        void TryShoot()
        {
            if (uiManagerScript.GetStartShootingBool() == false) return;
            if (inputManagerScript.Fire() == false) return;
            if (equippedWeapon == false) return;
            if (timeSinceLastShot < reloadTime) return;

            timeSinceLastShot = 0;
            audioManagerScript.PlayShootingClip();
            StartCoroutine(ShootParticleVFX());

            if (Physics.Raycast(GetRay(), out RaycastHit hit, float.MaxValue, targetLayerMask))
            {
                hitInfo = hit;
                hit.transform.gameObject.SetActive(false);
                StartCoroutine(HitTargetDecal());

                if (hit.transform.gameObject.tag == "Bomb")
                {
                    audioManagerScript.PlayBombHitClip();
                    scoreManagerScript.ModifyScore(pointsOnHitBomb);
                }
                else
                {
                    audioManagerScript.PlayTargetHitClip();
                    scoreManagerScript.ModifyScore(pointsOnHit);
                }
            }
            else
            {
                scoreManagerScript.ModifyScore(pointsOnMiss);
            }
        }

        IEnumerator HitTargetDecal()
        {
            var offset = new Vector3(0, .1f, 0);
            var hitDecal = Instantiate(impactDecal, hitInfo.transform.position + offset, Quaternion.identity);
            yield return new WaitForSeconds(1);
            Destroy(hitDecal);
        }

        IEnumerator ShootParticleVFX()
        {
            shotParticle.gameObject.SetActive(true);
            yield return new WaitForSeconds(.1f);
            shotParticle.gameObject.SetActive(false);
        }

        Ray GetRay() => new Ray(_camera.transform.position, _camera.transform.forward);

        void UpdatePlayerPositionAndRotation()
        {
            if (uiManagerScript.GetStartShootingBool()) return;
            var positionVector = Vector3.zero;
            var xRestriction = 1.6f;
            positionVector.x = inputManagerScript.Movement();
            transform.position += positionVector * movementSpeed * Time.deltaTime;
            var restrictedPosition = Mathf.Clamp(transform.position.x, -xRestriction, xRestriction);
            transform.position = new Vector3(restrictedPosition, 0, -2);
        }
    }
}
