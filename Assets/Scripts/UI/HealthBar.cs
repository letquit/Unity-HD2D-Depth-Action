using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class HealthBar : MonoBehaviour
{
    [SerializeField] Image _healthBarFillImage;
    [SerializeField] Image _healthBarTrailingFillImage;
    [SerializeField] float _trailDelay = 0.4f;
    [SerializeField] float _maxhealth = 100f;

    private float _currentHealth;

    private Sequence _currentSequence;

    private void Awake()
    {
        _currentHealth = _maxhealth;
        _healthBarFillImage.fillAmount = 1f;
        _healthBarTrailingFillImage.fillAmount = 1f;
    }

    public void SetMaxHealth(float maxHealth)
    {
        _maxhealth = maxHealth;
        _currentHealth = maxHealth;
        _healthBarFillImage.fillAmount = 1f;
        _healthBarTrailingFillImage.fillAmount = 1f;
    }

    public void SetHealth(float health)
    {
        _currentHealth = Mathf.Clamp(health, 0, _maxhealth);
        float ratio = _currentHealth / _maxhealth;

        KillSequence();

        _currentSequence = DOTween.Sequence();
        _currentSequence.Append(_healthBarFillImage.DOFillAmount(ratio, 0.25f).SetEase(Ease.InOutSine));
        _currentSequence.AppendInterval(_trailDelay);
        _currentSequence.Append(_healthBarTrailingFillImage.DOFillAmount(ratio, 0.3f).SetEase(Ease.InOutSine));
        _currentSequence.Play();
    }
    
    public void KillSequence()
    {
        if (_currentSequence != null && _currentSequence.IsActive())
        {
            _currentSequence.Kill();
            _currentSequence = null;
        }
    }

    private void OnDestroy()
    {
        KillSequence();
    }
}