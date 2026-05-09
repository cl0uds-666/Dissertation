using UnityEngine;

public class EnemyShooter : MonoBehaviour
{
    public Transform player;
    public bool canShoot = true;
    public float damage = 10f;
    public float fireCooldown = 1.5f;
    public float shotRange = 35f;
    public float shotRadius = 0.15f;
    public float shotSpread = 0.2f;
    public float enemyEyeHeight = 1.3f;
    public float playerAimHeight = 1.2f;
    [Range(0f, 1f)] public float peekDamageChance = 0.5f;
    public bool useCoverPeek;
    public float hideDuration = 1.8f;
    public float peekDuration = 1.2f;
    public float hiddenHeightOffset = -0.8f;

    private float nextFireTime; private float stateTimer; private bool isPeeking = true; private Vector3 baseLocalPos; private EnemyVision vision;
    private void Awake(){baseLocalPos=transform.localPosition; vision=GetComponent<EnemyVision>();}

    public void Setup(Transform playerTransform,float newDamage,float newFireCooldown,float newShotRange,float newShotRadius,float newShotSpread,bool shootingEnabled,float newPeekDamageChance,float newHideDuration,float newPeekDuration,bool newUseCoverPeek)
    { player=playerTransform; damage=newDamage; fireCooldown=newFireCooldown; shotRange=newShotRange; shotRadius=newShotRadius; shotSpread=newShotSpread; canShoot=shootingEnabled; peekDamageChance=Mathf.Clamp01(newPeekDamageChance); hideDuration=newHideDuration; peekDuration=newPeekDuration; useCoverPeek=newUseCoverPeek; }

    private void Update()
    {
        if (useCoverPeek){stateTimer += Time.deltaTime; float limit = isPeeking ? peekDuration : hideDuration; if(stateTimer>=limit){ stateTimer=0f; isPeeking=!isPeeking; transform.localPosition = baseLocalPos + Vector3.up * (isPeeking ? 0f : hiddenHeightOffset);} }
        if (!canShoot || player==null || Time.time<nextFireTime) return;
        if (useCoverPeek && !isPeeking) return;
        if (vision != null && !vision.CanSeePlayer) return;
        TryShootPlayer(); nextFireTime=Time.time+fireCooldown;
    }
    private void TryShootPlayer(){ Vector3 o=transform.position+Vector3.up*enemyEyeHeight; Vector3 t=player.position+Vector3.up*playerAimHeight; Vector3 d=(t-o); if(d.magnitude>shotRange) return; Vector3 dir=ApplySpread(d.normalized); if(!Physics.SphereCast(o,shotRadius,dir,out RaycastHit hit,shotRange)) return; PlayerHealth ph=hit.collider.GetComponentInParent<PlayerHealth>(); if(ph==null) return; PlayerCoverController c=ph.GetComponent<PlayerCoverController>(); if(c==null || !c.IsInCover){ph.TakeDamage(damage); return;} if(c.IsPeekingFromCover && Random.value<=peekDamageChance) ph.TakeDamage(damage); }
    private Vector3 ApplySpread(Vector3 originalDirection){ return (originalDirection + new Vector3(Random.Range(-shotSpread, shotSpread),Random.Range(-shotSpread, shotSpread),Random.Range(-shotSpread, shotSpread))).normalized; }
}
