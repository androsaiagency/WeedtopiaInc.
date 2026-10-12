using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Weedtopia.Domain;

namespace Weedtopia.Unity
{
    /// <summary>Presentation and responsive placement of explicitly authored menu, dialogue and profile controls.</summary>
    public sealed class UiExperience : MonoBehaviour
    {
        public RectTransform safeArea;
        public Text logo, subtitle, playHint;
        public Image emblem;
        public Image titleBackground, titleWordmark;
        public DialoguePanel intro, conversation;
        public QuestTracker quests;
        public Button gameTab, profileTab, applyProfile, previousAvatar, nextAvatar;
        public Button startSettings, copyDiagnostics;
        public ScrollRect gameScroll, profileScroll;
        public Dropdown language;
        public Text languageLabel;
        public RectTransform gamePage, profilePage;
        public InputField profileName;
        public Image avatar;
        public PlayerAvatarCatalog avatars;
        public Text avatarCaption, profileFeedback;
        public Button confirmRestart, cancelRestart;
        public Text restartBody;
        public CanvasGroup inspectionVisibility;
        private GameRoot root;
        private int avatarIndex;
        private string profileFeedbackKey;
        private Func<DiagnosticsCopyResult> diagnosticsCopy = PlayerDiagnosticsClipboard.Copy;
        private float Pixels => UiDisplayMetrics.Scale(GetComponent<Canvas>());
        public bool OwnsLayout(GameObject panel) => new[]{"start","intro","conversation","settings","questPreview","questDetails","newGameConfirm"}.Contains(panel.name);
        public void Initialize(GameRoot value)
        {
            root=value;quests.Initialize(root);
            RefreshLanguage();
            language.onValueChanged.AddListener(index=>root.SetLanguage(root.Localization.Languages[index].Code));
            conversation.tapScreen.onClick.AddListener(root.Ui.AdvanceConversation);
            intro.tapScreen.onClick.AddListener(()=>{if(!root.Session.State.profileNamePending)NextIntro();});
            profileScroll.gameObject.SetActive(false);
            gameTab.onClick.AddListener(()=>SelectSettingsTab(false));profileTab.onClick.AddListener(()=>SelectSettingsTab(true));
            previousAvatar.onClick.AddListener(()=>ChangeAvatar(-1));nextAvatar.onClick.AddListener(()=>ChangeAvatar(1));
            applyProfile.onClick.AddListener(()=>
            {
                if(!PlayerProfile.ValidName(profileName.text)){SetProfileFeedback("presentation.uiexperience.bitte_gib_einen_namen_mit_2_24_zeichen_e.add3a32b");return;}
                var failure=root.ChangeProfile(profileName.text,PlayerProfile.Avatars[avatarIndex]);
                SetProfileFeedback(failure==Failure.None?"presentation.uiexperience.dein_profil_ist_gespeichert.7543ba9b":"presentation.uiexperience.das_profil_konnte_nicht_gespeichert_werd.9794b4e8");
            });
            confirmRestart.onClick.AddListener(()=>{if(root.RestartGame())root.Pause(false);});
            cancelRestart.onClick.AddListener(ShowSettings);
            if(startSettings!=null)startSettings.onClick.AddListener(ShowSettings);
            if(copyDiagnostics!=null)copyDiagnostics.onClick.AddListener(CopyDiagnostics);
        }
        public void RefreshLanguage()
        {
            language.ClearOptions();language.AddOptions(root.Localization.Languages.Select(l=>l.NativeName).ToList());
            language.SetValueWithoutNotify(root.Localization.Languages.ToList().FindIndex(l=>l.Code==root.Localization.Language));
            languageLabel.text=root.Definitions.Text("settings.language");
            gameTab.GetComponentInChildren<Text>().text=root.Definitions.Text("settings.game");
            profileTab.GetComponentInChildren<Text>().text=root.Definitions.Text("settings.profile");
            if(copyDiagnostics!=null)copyDiagnostics.GetComponentInChildren<Text>(true).text=root.Definitions.Text("settings.diagnostics.copy");
            SetProfileFeedback(profileFeedbackKey);
            if(root.Session!=null&&intro.gameObject.activeSelf)PaintIntro();
            if(root.Session!=null)PaintAvatar();
            Fit();
        }
        private void SetProfileFeedback(string key)
        {profileFeedbackKey=key;profileFeedback.text=string.IsNullOrEmpty(key)?"":GameText.Get(key);}
        private void ChangeAvatar(int delta)
        {avatarIndex=(avatarIndex+delta+PlayerProfile.Avatars.Length)%PlayerProfile.Avatars.Length;PaintAvatar();SetProfileFeedback("presentation.uiexperience.mit_profil_speichern_bernehmen.b791fda2");}
        private void PaintAvatar()
        {avatar.sprite=avatars.Portrait(PlayerProfile.Avatars[avatarIndex]);avatarCaption.text=GameText.Format("profile.avatar",avatarIndex+1,PlayerProfile.Avatars.Length);}
        public void ShowSettings()
        {
            if(root.ContinueBusy||root.TravelLoading)return;
            root.Pause(true);root.Ui.ShowPanel("settings");
            bool editable=root.Session!=null&&!root.ContinuationPending;
            root.Ui.Element<Slider>("volume").interactable=editable;
            root.Ui.Element<Slider>("scale").interactable=editable;
            root.Ui.Element<Button>("ui.restart_intro").interactable=editable;
            applyProfile.interactable=editable;
            if(root.Session!=null)
            {
                profileName.SetTextWithoutNotify(root.Session.State.playerName);
                avatarIndex=Mathf.Max(0,Array.IndexOf(PlayerProfile.Avatars,root.Session.State.playerAvatar));PaintAvatar();
            }
            SetProfileFeedback(null);SelectSettingsTab(false);
        }
        public void SelectSettingsTab(bool profile)
        {
            bool editable=root.Session!=null&&!root.ContinuationPending;
            profile=profile&&editable;
            gameScroll.gameObject.SetActive(!profile);profileScroll.gameObject.SetActive(profile);
            gameTab.interactable=profile;profileTab.interactable=editable&&!profile;Fit();
        }
        public void CopyDiagnostics()
        {
            var result=diagnosticsCopy();
            string key=result==DiagnosticsCopyResult.Copied?"settings.diagnostics.copied":
                result==DiagnosticsCopyResult.Empty?"settings.diagnostics.empty":"settings.diagnostics.failed";
            root.Ui.Notify(root.Definitions.Text(key));
        }
        public void AskRestart(){root.Ui.ShowPanel("newGameConfirm");}
        public void PaintIntro()
        {
            var state=root.Session.State;bool naming=state.profileNamePending;
            string text=root.Definitions.Text(naming?"intro.name":"intro."+Mathf.Clamp(state.introLine,0,2));
            if(!naming)text=string.Format(GameText.Current?.Culture ?? System.Globalization.CultureInfo.InvariantCulture,text,state.playerName);
            intro.Present("rocko",naming||state.introLine==0?"greeting":state.introLine==2?"pleased":"explaining",root.Definitions.Text("speaker.rocko"),text,naming);
            intro.next.GetComponentInChildren<Text>().text=naming?GameText.Get("presentation.uiexperience.so_hei_e_ich.7de1020d"):GameText.Get("ui.next");
            intro.skip.GetComponentInChildren<Text>().text=naming?GameText.Get("inventory.cancel"):GameText.Get("presentation.uiexperience.berspringen.8e463fdd");
        }
        public bool AcceptTutorialName()
        {
            if(!root.Session.State.profileNamePending)return true;
            if(!PlayerProfile.ValidName(intro.nameInput.text)){var label=intro.nameGroup.Find("NameCaption").GetComponent<Text>();label.text=GameText.Get("presentation.uiexperience.2_24_zeichen_bitte.2630373a");label.color=new Color(1,.7f,.5f);return false;}
            return root.ChangeProfile(intro.nameInput.text,root.Session.State.playerAvatar)==Failure.None;
        }
        public void NextIntro()
        {
            bool naming=root.Session.State.profileNamePending;
            if(!AcceptTutorialName())return;
            if(!naming&&!root.AdvanceIntro())return;
            if(root.Session.State.introSeen)root.Ui.Close();else PaintIntro();Fit();
        }
        public void SkipIntro()
        {
            if(root.Session.State.profileNamePending){root.SaveSilently();root.Ui.ShowStart();return;}
            if(root.CompleteIntro())root.Ui.Close();
        }
        private void LateUpdate(){if(root!=null)Fit();}
        public void Fit()
        {
            if(root==null)return;float px=Pixels,margin=Mathf.Max(24,12/px),font=Mathf.Max(28,18/px),button=Mathf.Max(72,48/px);
            var start=root.Ui.Element<RectTransform>("start");
            if(start.gameObject.activeSelf)
            {
                Center(start,safeArea.rect.width,safeArea.rect.height);
                float w=start.rect.width,h=start.rect.height;
                logo.text="WEEDTOPIA";logo.gameObject.SetActive(false);subtitle.gameObject.SetActive(false);emblem.gameObject.SetActive(false);playHint.gameObject.SetActive(root.ContinueBusy);
                float canvasScale=GetComponent<Canvas>().scaleFactor;
                float screenW=Screen.width/canvasScale,screenH=Screen.height/canvasScale;
                float cover=Mathf.Max(screenW/titleBackground.sprite.rect.width,screenH/titleBackground.sprite.rect.height);
                titleBackground.rectTransform.sizeDelta=titleBackground.sprite.rect.size*cover;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(start,new Vector2(Screen.width*.5f,Screen.height*.5f),null,out var center);
                titleBackground.rectTransform.position=start.TransformPoint(center);
                float logoWidth=Mathf.Min(w-4*margin,w*.72f),logoHeight=logoWidth*titleWordmark.sprite.rect.height/titleWordmark.sprite.rect.width;
                Place(titleWordmark.rectTransform,0,-Mathf.Max(logoHeight*.55f+margin,h*.25f),logoWidth,logoHeight);
                var play=root.Ui.Element<Button>("ui.start");Place((RectTransform)play.transform,0,-h*.80f,Mathf.Min(w-4*margin,Mathf.Max(520,240/px)),Mathf.Max(116,64/px));
                DialoguePanel.FitCaption(play,Mathf.Max(44,28/px));
                if(startSettings!=null)
                {
                    float size=Mathf.Max(80,48/px);
                    var rect=(RectTransform)startSettings.transform;
                    rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one;
                    rect.anchoredPosition=new Vector2(-margin,-margin);rect.sizeDelta=Vector2.one*size;
                    startSettings.interactable=!root.ContinueBusy&&!root.TravelLoading;
                    var icon=rect.Find("Icon") as RectTransform;
                    if(icon!=null)icon.sizeDelta=Vector2.one*size*.6f;
                }
                if(root.ContinueBusy)
                {
                    playHint.text=GameText.Format("ui.offline_progress",Mathf.Clamp(Mathf.RoundToInt(root.ContinueProgress*100),0,100));
                    playHint.fontSize=Mathf.RoundToInt(font*.85f);
                    Place(playHint.rectTransform,0,-h*.91f,w-4*margin,Mathf.Max(font*2,button*.7f));
                }
            }
            if(intro.gameObject.activeSelf)intro.Fit(safeArea,px);
            if(conversation.gameObject.activeSelf)conversation.Fit(safeArea,px);
            foreach(string id in new[]{"settings","questDetails","newGameConfirm"})
            {
                var panel=root.Ui.Element<RectTransform>(id);if(!panel.gameObject.activeSelf)continue;
                Center(panel,Mathf.Min(1320,safeArea.rect.width-2*margin),Mathf.Min(1040,safeArea.rect.height-2*margin));
                if(id=="settings")FitSettings(panel,font,button,margin);
                if(id=="newGameConfirm")
                {
                    var title=root.Ui.Element<Text>("newGameConfirmTitle");title.text=GameText.Get("presentation.uiexperience.neues_spiel.12224074");title.fontSize=Mathf.RoundToInt(font*1.15f);float titleWidth=panel.rect.width-4*margin-button,titleHeight=DialoguePanel.PreferredHeight(title,titleWidth);Place(title.rectTransform,-button*.5f,-margin-titleHeight*.5f,titleWidth,titleHeight);
                    restartBody.fontSize=Mathf.RoundToInt(font);Place(restartBody.rectTransform,0,-panel.rect.height*.45f,panel.rect.width-4*margin,panel.rect.height*.38f);
                    float w=(panel.rect.width-4*margin-20)*.5f;
                    Place((RectTransform)confirmRestart.transform,-w*.5f-10,-panel.rect.height+margin+button*.5f,w,button);
                    Place((RectTransform)cancelRestart.transform,w*.5f+10,-panel.rect.height+margin+button*.5f,w,button);
                    confirmRestart.GetComponentInChildren<Text>().text=GameText.Get("presentation.uiexperience.neu_starten.3611a239");cancelRestart.GetComponentInChildren<Text>().text=GameText.Get("ui.cancel");
                    foreach(var action in new[]{confirmRestart,cancelRestart})DialoguePanel.FitCaption(action,font*.9f);
                }
                FitClose(panel,px);
            }
            quests.Fit(safeArea,px);FitInteraction(px);
            foreach(var frame in GetComponentsInChildren<ModalFrame>()){frame.Fit(safeArea,px);frame.GetComponent<TeamPanelLayout>()?.Fit(root,px);frame.GetComponent<ModulePanelLayout>()?.Fit(px);frame.GetComponent<ShopPanelLayout>()?.Fit(px);FitClose((RectTransform)frame.transform,px);}
            if(inspectionVisibility!=null){bool shown=!root.Ui.Interaction.PickerOpen;inspectionVisibility.alpha=shown?1:0;inspectionVisibility.blocksRaycasts=shown;inspectionVisibility.interactable=shown;}
        }
        private static void Center(RectTransform panel,float width,float height)
        {panel.anchorMin=panel.anchorMax=panel.pivot=new Vector2(.5f,.5f);panel.localScale=Vector3.one;panel.anchoredPosition=Vector2.zero;panel.sizeDelta=new Vector2(width,height);}
        private static void Place(RectTransform rect,float x,float y,float width,float height)=>DialoguePanel.Place(rect,new Vector2(.5f,1),new Vector2(x,y),new Vector2(width,height));
        public void FitClose(RectTransform panel,float px)
        {
            var close=root.Ui.Element<RectTransform>("ui.close");if(!close.gameObject.activeSelf)return;
            float size=Mathf.Max(76,48/px),inset=Mathf.Max(28,14/px);
            close.anchorMin=close.anchorMax=close.pivot=new Vector2(.5f,.5f);close.localScale=Vector3.one;close.sizeDelta=new Vector2(size,size);
            Vector2 center=close.parent.InverseTransformPoint(panel.TransformPoint(panel.rect.center));
            close.anchoredPosition=center+new Vector2(panel.rect.width*panel.localScale.x*.5f-size*.5f-inset,panel.rect.height*panel.localScale.y*.5f-size*.5f-inset);
            close.SetAsLastSibling();
            close.GetComponent<CloseButtonVisual>()?.Fit();
        }
        private void FitLanguagePopup(float rowHeight,float margin)
        {
            var control=(RectTransform)language.transform;
            // Unity's Dropdown flips an overflowing list once; that can put its first rows
            // above the screen. Size the viewport to the actual space on its chosen side.
            Vector3 safeBottom=control.InverseTransformPoint(safeArea.TransformPoint(new Vector3(0,safeArea.rect.yMin,0)));
            Vector3 safeTop=control.InverseTransformPoint(safeArea.TransformPoint(new Vector3(0,safeArea.rect.yMax,0)));
            float below=Mathf.Max(0,control.rect.yMin-safeBottom.y-margin);
            float above=Mathf.Max(0,safeTop.y-control.rect.yMax-margin);
            bool opensAbove=above>below;
            float height=Mathf.Min(rowHeight*language.options.Count+8,Mathf.Max(1,(opensAbove?above:below)-2));
            var template=language.template;
            template.anchorMin=new Vector2(0,opensAbove?1:0);
            template.anchorMax=new Vector2(1,opensAbove?1:0);
            template.pivot=new Vector2(.5f,opensAbove?0:1);
            template.anchoredPosition=new Vector2(0,opensAbove?2:-2);
            template.sizeDelta=new Vector2(0,height);

            var scroll=template.GetComponent<ScrollRect>();
            var item=template.GetComponentInChildren<Toggle>(true);
            if(item!=null)
            {
                var row=(RectTransform)item.transform;
                row.anchorMin=new Vector2(0,.5f);row.anchorMax=new Vector2(1,.5f);
                row.pivot=new Vector2(.5f,.5f);row.anchoredPosition=Vector2.zero;
                row.sizeDelta=new Vector2(0,rowHeight);
            }
            if(scroll!=null)
            {
                // Dropdown derives its item padding from this template, before cloning it.
                scroll.content.sizeDelta=new Vector2(0,rowHeight+8);
                scroll.horizontal=false;scroll.vertical=true;
                scroll.movementType=ScrollRect.MovementType.Clamped;
                scroll.scrollSensitivity=rowHeight*.5f;
                scroll.verticalNormalizedPosition=1;
                if(scroll.verticalScrollbar!=null)scroll.verticalScrollbar.value=1;
            }
        }
        private void FitSettings(RectTransform panel,float font,float button,float margin)
        {
            float w=panel.rect.width-4*margin;
            var title=root.Ui.Element<Text>("settingsTitle");title.text=GameText.Get("ui.pause");title.fontSize=Mathf.RoundToInt(font);Place(title.rectTransform,-button*.5f,-margin-font,w-button,font*2);
            Place((RectTransform)gameTab.transform,-w*.25f,-margin-font*2.8f,w*.48f,button);
            Place((RectTransform)profileTab.transform,w*.25f,-margin-font*2.8f,w*.48f,button);
            foreach(var tab in new[]{gameTab,profileTab})tab.GetComponentInChildren<Text>().fontSize=Mathf.RoundToInt(font);
            foreach(var scroll in new[]{gameScroll,profileScroll})
            {var rect=(RectTransform)scroll.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(2*margin,margin);rect.offsetMax=new Vector2(-2*margin,-margin-font*2.8f-button*.65f);}
            float y=font;
            Place(languageLabel.rectTransform,0,-y-font*.7f,w-20,font*1.4f);languageLabel.fontSize=Mathf.RoundToInt(font);y+=font*1.85f;
            Place((RectTransform)language.transform,0,-y-button*.5f,w-20,button);y+=button+font*.45f;
            foreach(var text in language.GetComponentsInChildren<Text>(true))text.fontSize=Mathf.RoundToInt(font);
            FitLanguagePopup(button,margin);

            foreach(string id in new[]{"volumeLabel","volume","scaleLabel","scale","ui.resume","ui.restart_intro","ui.reset"})
            {
                var target=root.Ui.Element<RectTransform>(id);bool label=id.EndsWith("Label",StringComparison.Ordinal);
                foreach(var text in target.GetComponentsInChildren<Text>(true))text.fontSize=Mathf.RoundToInt(font);
                float height=label?font*1.4f:button;
                var caption=target.GetComponentInChildren<Text>(true);
                if(caption!=null)
                {
                    var icon=(target.Find("ActionIcon")??target.Find("Icon")) as RectTransform;
                    float leftInset=icon==null?12:Mathf.Max(font*2.2f,icon.anchoredPosition.x+icon.rect.width*.5f+font*.4f);
                    if(!label){caption.rectTransform.offsetMin=new Vector2(leftInset,4);caption.rectTransform.offsetMax=new Vector2(-12,-4);}
                    height=Mathf.Max(height,DialoguePanel.PreferredHeight(caption,w-20-leftInset-12)+8);
                }
                Place(target,0,-y-height*.5f,w-20,height);y+=height+font*.45f;
            }
            if(copyDiagnostics!=null)
            {
                var caption=copyDiagnostics.GetComponentInChildren<Text>(true);caption.fontSize=Mathf.RoundToInt(font);
                float height=Mathf.Max(button,DialoguePanel.PreferredHeight(caption,w-52)+16);
                Place((RectTransform)copyDiagnostics.transform,0,-y-height*.5f,w-20,height);y+=height+font*.45f;
            }
            gamePage.sizeDelta=new Vector2(0,y+font);
            float avatarHeight=Mathf.Max(180,85/Pixels);
            Place(avatar.rectTransform,0,-avatarHeight*.5f,w*.45f,avatarHeight);
            Place((RectTransform)previousAvatar.transform,-w*.35f,-avatarHeight*.5f,button,button);Place((RectTransform)nextAvatar.transform,w*.35f,-avatarHeight*.5f,button,button);
            avatarCaption.fontSize=Mathf.RoundToInt(font*.9f);Place(avatarCaption.rectTransform,0,-avatarHeight-font,w,font*1.6f);
            foreach(var action in new[]{previousAvatar,nextAvatar})action.GetComponentInChildren<Text>().fontSize=Mathf.RoundToInt(font);
            Place((RectTransform)profileName.transform,0,-avatarHeight-font*2.8f-button*.5f,w-20,button);
            foreach(var text in profileName.GetComponentsInChildren<Text>(true))text.fontSize=Mathf.RoundToInt(font);
            Place((RectTransform)applyProfile.transform,0,-avatarHeight-font*3.5f-button*1.6f,w-20,button);applyProfile.GetComponentInChildren<Text>().fontSize=Mathf.RoundToInt(font);
            profileFeedback.fontSize=Mathf.RoundToInt(font*.85f);Place(profileFeedback.rectTransform,0,-avatarHeight-font*5.4f-button*2,w-20,font*2.8f);
            profilePage.sizeDelta=new Vector2(0,avatarHeight+font*7+button*2);
        }
        private void FitInteraction(float px)
        {
            var ui=root.Ui.Interaction;if(root.Session==null||ui==null)return;
            float margin=Mathf.Max(24,12/px),top=margin,button=Mathf.Max(70,48/px),font=Mathf.Max(25,17/px);
            if(ui.BuildingOpen)
            {
                ui.inspector.GetComponent<BuildingInspectorLayout>()?.Fit(safeArea,px);
            }
            if(!ui.PickerOpen)return;
            var picker=(RectTransform)ui.picker.transform;float width=Mathf.Min(safeArea.rect.width-2*margin,Mathf.Max(900,400/px)),height=safeArea.rect.height-top-margin;
            picker.anchorMin=picker.anchorMax=picker.pivot=new Vector2(1,1);picker.anchoredPosition=new Vector2(-margin,-top);picker.localScale=Vector3.one;picker.sizeDelta=new Vector2(width,height);
            ui.pickerTitle.fontSize=Mathf.RoundToInt(font);Place(ui.pickerTitle.rectTransform,-button*.5f,-font*2.1f,width-2*margin-button,font*3.4f);
            var closePicker=(RectTransform)ui.closePicker.transform;closePicker.anchorMin=closePicker.anchorMax=closePicker.pivot=Vector2.one;closePicker.anchoredPosition=new Vector2(-margin,-margin);closePicker.sizeDelta=new Vector2(button,button);closePicker.SetAsLastSibling();
            FitIcon(ui.closePicker,button);
            ui.pickerHint.fontSize=Mathf.RoundToInt(Mathf.Max(font*.9f,16/px));float hint=ui.pickerHint.cachedTextGeneratorForLayout.GetPreferredHeight(ui.pickerHint.text,ui.pickerHint.GetGenerationSettings(new Vector2(width-2*margin,0)))/ui.pickerHint.pixelsPerUnit+12;Place(ui.pickerHint.rectTransform,0,-height+hint*.5f+margin,width-2*margin,hint);
            var scroll=(RectTransform)ui.pickerScroll.transform;scroll.anchorMin=Vector2.zero;scroll.anchorMax=Vector2.one;scroll.offsetMin=new Vector2(margin,margin+hint);scroll.offsetMax=new Vector2(-margin,-font*4.2f-margin);
            foreach(var row in ui.pickerContent.GetComponentsInChildren<InteractionRow>())
            {
                float inner=width-2*margin,textWidth=inner-140;
                row.title.fontSize=Mathf.RoundToInt(font*.9f);row.body.fontSize=Mathf.RoundToInt(font*.85f);row.choose.GetComponentInChildren<Text>().fontSize=Mathf.RoundToInt(font*.9f);
                float bodyHeight=row.body.cachedTextGeneratorForLayout.GetPreferredHeight(row.body.text,row.body.GetGenerationSettings(new Vector2(textWidth-20,0)))/row.body.pixelsPerUnit;
                float rowHeight=Mathf.Max(300,font*2.8f+bodyHeight+button+40);row.GetComponent<LayoutElement>().preferredHeight=rowHeight;
                Place(row.title.rectTransform,60,-font*1.3f,textWidth-20,font*2.4f);Place(row.body.rectTransform,60,-font*2.8f-bodyHeight*.5f,textWidth-20,bodyHeight+10);
                Place(row.portrait.rectTransform,-inner*.5f+70,-font*3.8f,120,font*5.5f);
                float chooseWidth=row.remove.gameObject.activeSelf?inner-button-20:inner-20;
                Place((RectTransform)row.choose.transform,row.remove.gameObject.activeSelf?-button*.5f:0,-rowHeight+button*.5f+12,chooseWidth,button);
                var actionIcon=row.choose.transform.Find("Icon") as RectTransform;
                if(actionIcon!=null)DialoguePanel.Place(actionIcon,new Vector2(0,.5f),new Vector2(button*.5f,0),Vector2.one*(button*.55f));
                var actionCaption=row.choose.GetComponentInChildren<Text>().rectTransform;
                actionCaption.anchorMin=Vector2.zero;actionCaption.anchorMax=Vector2.one;actionCaption.offsetMin=new Vector2(button+12,8);actionCaption.offsetMax=new Vector2(-button-12,-8);
                Place((RectTransform)row.remove.transform,inner*.5f-button*.5f-10,-rowHeight+button*.5f+12,button,button);
            }
        }
        private static void FitIcon(Button button,float size)
        {
            var close=button.GetComponent<CloseButtonVisual>();if(close!=null){close.Fit();return;}
            var icon=button.transform.Find("Icon")?.GetComponent<RectTransform>();
            if(icon!=null)DialoguePanel.Place(icon,new Vector2(.5f,.5f),Vector2.zero,Vector2.one*size*.58f);
        }
    }
}
