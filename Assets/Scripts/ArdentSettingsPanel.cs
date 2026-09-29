using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ArdentSettingsPanel : MonoBehaviour
{
    public Slider rotationSlider, zoomSlider, cardGapSlider;
    public Toggle showCardsToggle;
    public TMP_Text rotationValue, zoomValue, cardGapValue;
    public Button restoreDefaultsButton;
    private void OnEnable()
    {
        Refresh();
        if(rotationSlider!=null) rotationSlider.onValueChanged.AddListener(ChangeRotation);
        if(zoomSlider!=null) zoomSlider.onValueChanged.AddListener(ChangeZoom);
        if(cardGapSlider!=null) cardGapSlider.onValueChanged.AddListener(ChangeGap);
        if(showCardsToggle!=null) showCardsToggle.onValueChanged.AddListener(ChangeCards);
        if(restoreDefaultsButton!=null) restoreDefaultsButton.onClick.AddListener(ResetSettings);
    }
    private void OnDisable()
    {
        if(rotationSlider!=null) rotationSlider.onValueChanged.RemoveListener(ChangeRotation);
        if(zoomSlider!=null) zoomSlider.onValueChanged.RemoveListener(ChangeZoom);
        if(cardGapSlider!=null) cardGapSlider.onValueChanged.RemoveListener(ChangeGap);
        if(showCardsToggle!=null) showCardsToggle.onValueChanged.RemoveListener(ChangeCards);
        if(restoreDefaultsButton!=null) restoreDefaultsButton.onClick.RemoveListener(ResetSettings);
    }
    private void Refresh()
    {
        SetSlider(rotationSlider,.25f,2f,ArdentSettings.Rotation);
        SetSlider(zoomSlider,.25f,2f,ArdentSettings.Zoom);
        SetSlider(cardGapSlider,.01f,.30f,ArdentSettings.CardGap);
        if(showCardsToggle!=null) showCardsToggle.SetIsOnWithoutNotify(ArdentSettings.ShowCards);
        if(rotationValue!=null) rotationValue.text=ArdentSettings.Rotation.ToString("0.00")+"x";
        if(zoomValue!=null) zoomValue.text=ArdentSettings.Zoom.ToString("0.00")+"x";
        if(cardGapValue!=null) cardGapValue.text=(ArdentSettings.CardGap*100).ToString("0")+" cm";
        if(cardGapSlider!=null) cardGapSlider.interactable=ArdentSettings.ShowCards;
    }
    private static void SetSlider(Slider s,float min,float max,float value)
    { if(s==null)return;s.minValue=min;s.maxValue=max;s.wholeNumbers=false;s.SetValueWithoutNotify(value); }
    private void ChangeRotation(float v){ArdentSettings.SetRotation(v);Refresh();}
    private void ChangeZoom(float v){ArdentSettings.SetZoom(v);Refresh();}
    private void ChangeGap(float v){ArdentSettings.SetCardGap(v);Refresh();}
    private void ChangeCards(bool v){ArdentSettings.SetShowCards(v);Refresh();}
    private void ResetSettings(){ArdentSettings.RestoreDefaults();Refresh();}
}
