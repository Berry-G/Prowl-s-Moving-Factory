using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PMF.Session;

namespace PMF.UI
{
    /// <summary>목적: 게임/그래픽/사운드 설정 UI. 구조: 공통 저장소에 위임. 불변: 메뉴는 GameClock 일시정지 유지. 근거: ADR-0023.</summary>
    public sealed partial class PauseMenu
    {
        private readonly List<GameObject> _settingsTabs = new List<GameObject>();
        private readonly List<Button> _tabButtons = new List<Button>();
        private readonly List<System.Action> _settingsRefresh = new List<System.Action>();
        private void OnEnable() => UserSettings.Changed += RefreshSettingsControls;
        private void OnDisable() { UserSettings.Changed -= RefreshSettingsControls; IsOpen = false; }
        private void RefreshSettingsControls() { foreach(var refresh in _settingsRefresh) refresh(); }
        private static readonly Vector2 Top = new Vector2(0.5f,1f);

        private void BuildTabbedSettings()
        {
            _settingsPanel = new GameObject("SettingsPanel",typeof(RectTransform),typeof(Image));
            _settingsPanel.transform.SetParent(transform,false);
            Rect(_settingsPanel,Top-new Vector2(0,0.5f),Top-new Vector2(0,0.5f),Vector2.zero,new Vector2(680,650));
            _settingsPanel.GetComponent<Image>().color=new Color(0.10f,0.12f,0.15f,0.99f);
            SettingsText(_settingsPanel.transform,"Title","환경설정",30,0,42,600,42);
            string[] names={"게임","그래픽","사운드"};
            for(int i=0;i<3;i++)
            {
                int index=i;
                var tab=SettingsButton(_settingsPanel.transform,"Tab_"+i,names[i],(i-1)*200,98,190,42);
                tab.onClick.AddListener(()=>SelectSettingsTab(index)); _tabButtons.Add(tab);
                var page=new GameObject("SettingsTab_"+i,typeof(RectTransform));
                page.transform.SetParent(_settingsPanel.transform,false);
                Rect(page,Top,Top,new Vector2(0,-355),new Vector2(610,440)); _settingsTabs.Add(page);
            }
            BuildGameSettings(_settingsTabs[0].transform);
            BuildGraphicsSettings(_settingsTabs[1].transform);
            BuildSoundSettings(_settingsTabs[2].transform);
            SettingsText(_settingsPanel.transform,"SavedHint","변경사항은 즉시 적용·저장됩니다",16,0,578,600,28);
            SettingsButton(_settingsPanel.transform,"Btn_CloseSettings","돌아가기",0,615,200,38).onClick.AddListener(()=>
            { _settingsPanel.SetActive(false); _panel.SetActive(true); });
            SelectSettingsTab(0); _settingsPanel.SetActive(false);
        }
        private void SelectSettingsTab(int index)
        {
            for(int i=0;i<_settingsTabs.Count;i++)
            {
                _settingsTabs[i].SetActive(i==index);
                _tabButtons[i].GetComponent<Image>().color=i==index ? new Color(0.25f,0.49f,0.70f) : new Color(0.20f,0.24f,0.30f);
            }
            foreach(var refresh in _settingsRefresh) refresh();
        }
        private void BuildGameSettings(Transform page)
        {
            var edge=SettingsButton(page,"EdgeScrolling","",0,34,530,42);
            System.Action refresh=()=>edge.GetComponentInChildren<Text>().text="화면 가장자리 이동: "+(UserSettings.Current.EdgeScrolling?"켜짐":"꺼짐");
            edge.onClick.AddListener(()=>{UserSettings.Change(s=>s.EdgeScrolling=!s.EdgeScrolling);refresh();}); _settingsRefresh.Add(refresh);
            SettingsText(page,"CameraHelp","휠: 확대·축소  /  가운데 버튼 드래그: 이동\n화면 모서리: 대각선 이동  /  UI 위에서는 카메라 입력 중지",18,0,113,570,72);
            // 왜: 기존 난이도/재시작 기능은 유지하되 새 난이도 선택 화면은 만들지 않는다.
            var outer=_settingsPanel; _settingsPanel=page.gameObject;
            BuildDifficultyRow();
            SettingsButton(page,"Btn_Restart","현재 스테이지 다시 시작",0,363,300,42).onClick.AddListener(OnRestart);
            _difficultyConfirm.transform.SetAsLastSibling(); _settingsPanel=outer;
        }
        private void BuildGraphicsSettings(Transform page)
        {
            SettingsText(page,"ResolutionLabel","해상도",20,-195,24,160,32);
            var sizes=UserSettings.AvailableResolutions();
            var labels=new List<string>(); foreach(var s in sizes) labels.Add(s.x+" × "+s.y);
            var resolution=SettingsDropdown(page,"Resolution",labels,65);
            resolution.onValueChanged.AddListener(i=> {if(i>=0&&i<sizes.Count)UserSettings.Change(s=>{s.Width=sizes[i].x;s.Height=sizes[i].y;});});
            SettingsText(page,"FpsLabel","FPS 제한",20,-195,120,160,32);
            int[] rates={60,120,144,165,-1};
            var fps=SettingsDropdown(page,"Fps",new List<string>{"60","120","144","165","무제한"},161);
            fps.onValueChanged.AddListener(i=>UserSettings.Change(s=>s.Fps=rates[i]));
            var vsync=SettingsButton(page,"VSync","",0,226,530,42);
            var hint=SettingsText(page,"VSyncHint","",17,0,287,570,60);
            System.Action refresh=()=>
            {
                var s=UserSettings.Current;
                int index=sizes.IndexOf(new Vector2Int(s.Width,s.Height));
                if(index<0)index=sizes.IndexOf(new Vector2Int(Screen.width,Screen.height));
                resolution.SetValueWithoutNotify(Mathf.Max(0,index));
                fps.SetValueWithoutNotify(System.Array.IndexOf(rates,s.Fps)); fps.interactable=!s.VSync;
                vsync.GetComponentInChildren<Text>().text="수직 동기화 (VSync): "+(s.VSync?"켜짐":"꺼짐");
                hint.text=s.VSync?"모니터 주사율에 동기화합니다.\nFPS 선택값은 VSync를 끄면 다시 적용됩니다.":"선택한 FPS를 최대 목표값으로 사용합니다.";
            };
            vsync.onClick.AddListener(()=>{UserSettings.Change(s=>s.VSync=!s.VSync);refresh();}); _settingsRefresh.Add(refresh);
#if UNITY_EDITOR
            SettingsText(page,"EditorHint","Unity 편집기에서는 해상도를 저장만 합니다.\n실제 화면 크기 변경은 실행 파일에서 적용됩니다.",16,0,370,570,60);
#endif
        }
        private void BuildSoundSettings(Transform page)
        {
            VolumeRow(page,"Master","전체 (Master)",40,s=>s.Master,(s,v)=>s.Master=v);
            VolumeRow(page,"BGM","배경음악 (BGM)",130,s=>s.Bgm,(s,v)=>s.Bgm=v);
            VolumeRow(page,"SFX","효과음 (SFX)",220,s=>s.Sfx,(s,v)=>s.Sfx=v);
            var mute=SettingsButton(page,"Mute","",0,338,530,42);
            System.Action refresh=()=>mute.GetComponentInChildren<Text>().text="전체 음소거: "+(UserSettings.Current.Muted?"켜짐":"꺼짐");
            mute.onClick.AddListener(()=>{UserSettings.Change(s=>s.Muted=!s.Muted);refresh();}); _settingsRefresh.Add(refresh);
            SettingsText(page,"SoundHint","음소거를 해제하면 선택한 볼륨으로 돌아옵니다.",16,0,394,570,32);
        }
        private void VolumeRow(Transform page,string id,string name,float y,System.Func<SettingsData,float> get,System.Action<SettingsData,float> set)
        {
            var label=SettingsText(page,id+"Label",name,20,0,y,530,30);
            var slider=MakeSlider(page,id,Top,Top,new Vector2(0,-y-34),new Vector2(530,24));
            System.Action refresh=()=>{float v=get(UserSettings.Current);slider.SetValueWithoutNotify(v);label.text=name+"   "+Mathf.RoundToInt(v*100);};
            slider.onValueChanged.AddListener(v=>{UserSettings.Change(s=>set(s,v));label.text=name+"   "+Mathf.RoundToInt(v*100);});
            _settingsRefresh.Add(refresh);
        }
        private Text SettingsText(Transform parent,string id,string text,int size,float x,float y,float width,float height)
            => MakeText(parent,id,text,size,TextAnchor.MiddleCenter,Top,Top,new Vector2(x,-y),new Vector2(width,height)).GetComponent<Text>();
        private Button SettingsButton(Transform parent,string id,string text,float x,float y,float width,float height)
            => MakeButton(parent,id,text,TextAnchor.MiddleCenter,Top,Top,new Vector2(x,-y),new Vector2(width,height)).GetComponent<Button>();
        private Dropdown SettingsDropdown(Transform parent,string id,List<string> options,float y)
        {
            var go=new GameObject(id,typeof(RectTransform),typeof(Image),typeof(Dropdown));
            go.transform.SetParent(parent,false);Rect(go,Top,Top,new Vector2(0,-y),new Vector2(530,42));
            go.GetComponent<Image>().color=new Color(0.25f,0.3f,0.4f);
            var dropdown=go.GetComponent<Dropdown>(); dropdown.targetGraphic=go.GetComponent<Image>();
            SettingsText(go.transform,"Arrow","▾",20,240,21,24,36);
            dropdown.captionText=MakeText(go.transform,"Label","",20,TextAnchor.MiddleLeft,Vector2.zero,Vector2.one,new Vector2(12,0),new Vector2(-24,0)).GetComponent<Text>();
            var template=new GameObject("Template",typeof(RectTransform),typeof(Image),typeof(ScrollRect));
            template.transform.SetParent(go.transform,false); Rect(template,new Vector2(0,0),new Vector2(1,0),new Vector2(0,-102),new Vector2(0,200));
            template.GetComponent<Image>().color=new Color(0.16f,0.19f,0.24f);
            var viewport=new GameObject("Viewport",typeof(RectTransform),typeof(Image),typeof(Mask));
            viewport.transform.SetParent(template.transform,false);Rect(viewport,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);viewport.GetComponent<Mask>().showMaskGraphic=false;
            var content=new GameObject("Content",typeof(RectTransform));content.transform.SetParent(viewport.transform,false);
            Rect(content,Top,Top,new Vector2(0,-20),new Vector2(530,40));
            var item=new GameObject("Item",typeof(RectTransform),typeof(Image),typeof(Toggle));item.transform.SetParent(content.transform,false);
            Rect(item,new Vector2(0,0.5f),new Vector2(1,0.5f),Vector2.zero,new Vector2(0,40));item.GetComponent<Image>().color=new Color(0.2f,0.27f,0.36f);
            var toggle=item.GetComponent<Toggle>();toggle.targetGraphic=item.GetComponent<Image>();
            var itemText=MakeText(item.transform,"ItemLabel","",20,TextAnchor.MiddleLeft,Vector2.zero,Vector2.one,new Vector2(12,0),new Vector2(-24,0)).GetComponent<Text>();
            dropdown.itemText=itemText;dropdown.template=template.GetComponent<RectTransform>();
            var scroll=template.GetComponent<ScrollRect>();scroll.viewport=viewport.GetComponent<RectTransform>();scroll.content=content.GetComponent<RectTransform>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
            template.SetActive(false);dropdown.ClearOptions();dropdown.AddOptions(options);return dropdown;
        }
    }
}
