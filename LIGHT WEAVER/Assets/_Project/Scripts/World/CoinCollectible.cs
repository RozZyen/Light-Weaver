using System;
using UnityEngine;
using LightWeaver.Core;
using LightWeaver.Player;

namespace LightWeaver.World
{
    /// <summary>
    /// Mısır Scarab Altını (Oyun içi kalıcı para birimi).
    /// Oyuncu yaklaştığında (veya Mıknatıs yükseltmesiyle daha uzaktan) oyuncuya doğru çekilir ve toplanır.
    /// Zero-GC Update() disipliniyle çalışır.
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class CoinCollectible : MonoBehaviour
    {
        public static event Action<CoinCollectible, int> OnCoinCollected;

        public const string PoolKey = "PoolScarabCoin";

        [Header("Coin Settings")]
        [SerializeField] private int coinValue = 1;
        [SerializeField] private float baseMagnetRadius = 2.1f;
        [SerializeField] private float magnetPullSpeed = 12.5f;
        [SerializeField] private float bobAmplitude = 0.14f;
        [SerializeField] private float bobFrequency = 3.2f;

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer coinRenderer;

        private CircleCollider2D _triggerCollider;
        private Vector3 _anchorPosition;
        private float _phaseOffset;
        private bool _isCollected;
        private bool _isMagnetized;

        private void Awake()
        {
            _triggerCollider = GetComponent<CircleCollider2D>();
            _triggerCollider.isTrigger = true;
            if (_triggerCollider.radius < 0.42f) _triggerCollider.radius = 0.48f;
            if (coinRenderer == null) coinRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void OnEnable()
        {
            _isCollected = false;
            _isMagnetized = false;
            _anchorPosition = transform.position;
            _phaseOffset = (transform.position.x * 0.85f) % (Mathf.PI * 2f);
            if (coinRenderer != null)
            {
                coinRenderer.enabled = true;
                coinRenderer.color = Color.white;
            }
        }

        public void Configure(int value, Sprite coinSprite = null)
        {
            coinValue = Mathf.Max(1, value);
            if (coinRenderer != null && coinSprite != null)
            {
                coinRenderer.sprite = coinSprite;
            }
        }

        private void Update()
        {
            if (_isCollected) return;

            BatterySystem runner = BatterySystem.ActiveInstance;
            if (runner != null)
            {
                Vector3 runnerPos = runner.transform.position + new Vector3(0f, 0.7f, 0f);
                float magnetRadius = baseMagnetRadius + ProgressManager.GetCoinMagnetBonusRadius();
                float distSq = (runnerPos - transform.position).sqrMagnitude;

                if (_isMagnetized || distSq <= magnetRadius * magnetRadius)
                {
                    _isMagnetized = true;
                    transform.position = Vector3.MoveTowards(transform.position, runnerPos, magnetPullSpeed * Time.deltaTime);
                    if (distSq <= 0.36f)
                    {
                        Collect();
                    }
                    return;
                }
            }

            float bobY = Mathf.Sin(Time.time * bobFrequency + _phaseOffset) * bobAmplitude;
            transform.position = new Vector3(_anchorPosition.x, _anchorPosition.y + bobY, _anchorPosition.z);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isCollected) return;
            if (other.GetComponentInParent<RunnerMotor2D>() != null)
            {
                Collect();
            }
        }

        private void Collect()
        {
            if (_isCollected) return;
            _isCollected = true;
            OnCoinCollected?.Invoke(this, coinValue);
            gameObject.SetActive(false);
        }
    }
}
