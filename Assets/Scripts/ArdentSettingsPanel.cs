using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ArdentSettingsPanel : MonoBehaviour
{
    public Slider rotationSlider, zoomSlider, cardGapSlider, musicSlider, sfxSlider;
    public Toggle showCardsToggle;
    public TMP_Text rotationValue, zoomValue, cardGapValue, musicValue, sfxValue;
    public Button restoreDefaultsButton;
    private void OnEnable()
    {
        Refresh();
        if(rotationSlider!=null) rotationSlider.onValueChanged.AddListener(ChangeRotation);
        if(zoomSlider!=null) zoomSlider.onValueChanged.AddListener(ChangeZoom);
        if(cardGapSlider!=null) cardGapSlider.onValueChanged.AddListener(ChangeGap);
        if(musicSlider!=null) musicSlider.onValueChanged.AddListener(ChangeMusic);
        if(sfxSlider!=null) sfxSlider.onValueChanged.AddListener(ChangeSfx);
        if(showCardsToggle!=null) showCardsToggle.onValueChanged.AddListener(ChangeCards);
        if(restoreDefaultsButton!=null) restoreDefaultsButton.onClick.AddListener(ResetSettings);
    }
    private void OnDisable()
    {
        if(rotationSlider!=null) rotationSlider.onValueChanged.RemoveListener(ChangeRotation);
        if(zoomSlider!=null) zoomSlider.onValueChanged.RemoveListener(ChangeZoom);
        if(cardGapSlider!=null) cardGapSlider.onValueChanged.RemoveListener(ChangeGap);
        if(musicSlider!=null) musicSlider.onValueChanged.RemoveListener(ChangeMusic);
        if(sfxSlider!=null) sfxSlider.onValueChanged.RemoveListener(ChangeSfx);
        if(showCardsToggle!=null) showCardsToggle.onValueChanged.RemoveListener(ChangeCards);
        if(restoreDefaultsButton!=null) restoreDefaultsButton.onClick.RemoveListener(ResetSettings);
    }
    private void Refresh()
    {
        SetSlider(rotationSlider,.25f,2f,ArdentSettings.Rotation);
        SetSlider(zoomSlider,.25f,2f,ArdentSettings.Zoom);
        SetSlider(cardGapSlider,.01f,.30f,ArdentSettings.CardGap);
        SetSlider(musicSlider,0f,1f,ArdentSettings.MusicVolume);
        SetSlider(sfxSlider,0f,1f,ArdentSettings.SfxVolume);
        if(showCardsToggle!=null) showCardsToggle.SetIsOnWithoutNotify(ArdentSettings.ShowCards);
        if(rotationValue!=null) rotationValue.text=ArdentSettings.Rotation.ToString("0.00")+"x";
        if(zoomValue!=null) zoomValue.text=ArdentSettings.Zoom.ToString("0.00")+"x";
        if(cardGapValue!=null) cardGapValue.text=(ArdentSettings.CardGap*100).ToString("0")+" cm";
        if(musicValue!=null) musicValue.text=Mathf.RoundToInt(ArdentSettings.MusicVolume*100)+"%";
        if(sfxValue!=null) sfxValue.text=Mathf.RoundToInt(ArdentSettings.SfxVolume*100)+"%";
        if(cardGapSlider!=null) cardGapSlider.interactable=ArdentSettings.ShowCards;
    }
    private static void SetSlider(Slider s,float min,float max,float value)
    { if(s==null)return;s.minValue=min;s.maxValue=max;s.wholeNumbers=false;s.SetValueWithoutNotify(value); }
    private void ChangeRotation(float v){ArdentSettings.SetRotation(v);Refresh();}
    private void ChangeZoom(float v){ArdentSettings.SetZoom(v);Refresh();}
    private void ChangeGap(float v){ArdentSettings.SetCardGap(v);Refresh();}
    private void ChangeMusic(float v){ArdentSettings.SetMusicVolume(v);Refresh();}
    private void ChangeSfx(float v){ArdentSettings.SetSfxVolume(v);Refresh();}
    private void ChangeCards(bool v){ArdentSettings.SetShowCards(v);Refresh();}
    private void ResetSettings(){ArdentSettings.RestoreDefaults();Refresh();}
}
