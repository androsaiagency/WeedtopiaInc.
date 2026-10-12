using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Weedtopia.Unity
{
    public sealed class IsometricCameraRig : MonoBehaviour
    {
        [SerializeField] private Camera renderCamera;
        [SerializeField] private Vector2 minimum=new Vector2(-50,-20),maximum=new Vector2(60,60);
        [SerializeField] private float minZoom=12,maxZoom=55,panSpeed=25;
        [SerializeField] private DistrictMap districtMap;
        private DistrictRange district;
        private GameRoot root;
        private Vector3 target;
        private float zoom=28,pinchPrevious;
        private readonly WorldPointerGesture mouseGesture = new WorldPointerGesture(), touchGesture = new WorldPointerGesture();
        private bool inspection, suppressTouchUntilRelease;
        private Vector3 savedTarget;
        private float savedZoom;
        private DistrictRange savedDistrict;
        private BuildingView inspectedBuilding;
        private Bounds inspectionBounds;
        private int inspectionLevel, inspectionWidth, inspectionHeight;
        public bool IsInspecting => inspection;
        public float DesiredZoom => zoom;
        private readonly List<RaycastResult> uiHits=new List<RaycastResult>();
        public Camera RenderCamera=>renderCamera;
        public DistrictRange CurrentDistrict => district;
        public float DistrictFraction => district == null ? 0 : district.Fraction(DistrictMap.ToPlan(target));
        public Vector3 TargetPosition => target;
        public void Initialize(GameRoot value){root=value;target=transform.position;zoom=renderCamera.orthographicSize;}
        public void Focus(Vector3 position)
        {
            LeaveInspectionForGesture();
            target = ClampToWorld(new Vector3(position.x,0,position.z));
        }
        public void EnterDistrict(Weedtopia.Domain.Zone zone)
        {
            if(districtMap==null)return;
            LeaveInspectionForGesture();
            district=districtMap.Range(zone);
            zoom=Mathf.Clamp(district.zoom,minZoom,MaximumZoom);target=ClampToWorld(DistrictMap.ToWorld(district.start));
        }
        public void ShowOverview()
        {
            if(districtMap==null||root==null||root.Session==null)return;
            LeaveInspectionForGesture();
            district=null;zoom=MaximumZoom;target=DistrictMap.ToWorld(districtMap.overview);
        }
        public void ScrollTo(float fraction){if(district!=null)target=DistrictMap.ToWorld(Vector2.Lerp(district.start,district.end,Mathf.Clamp01(fraction)));}
        public void StepDistrict(int direction){if(district!=null)target=DistrictMap.ToWorld(district.Clamp(DistrictMap.ToPlan(target)+district.direction*direction*36));}
        public bool IsUiPoint(Vector2 position)
        {
            if(EventSystem.current==null)return false;
            uiHits.Clear();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=position},uiHits);
            return uiHits.Count>0;
        }
        public bool TrySelect(Vector2 position)
        {
            if(root==null||root.Session==null||root.Ui.ModalOpen||IsUiPoint(position))return false;
            var ray=renderCamera.ScreenPointToRay(position);
            CharacterActor character=null;BuildingView building=null;float nearest=float.MaxValue;int front=int.MinValue;
            // The visible 2.5D world uses authored boxes. Test these directly so selection stays
            // stable across long orthographic rays, static collider synchronization and scene reloads.
            if(root.Presentation!=null)foreach(var actor in root.Presentation.characters)
            {
                if(actor.spriteRenderer.enabled&&actor.spriteRenderer.sortingOrder>front&&actor.TryPick(ray,out _)){character=actor;front=actor.spriteRenderer.sortingOrder;}
            }
            foreach(var site in root.Sites)if(site.TryPick(ray,out float distance)&&distance<nearest){building=site;nearest=distance;}
            if(character!=null){root.Select(character.workplaceId);root.Ui.SpeakToSelected();return true;}
            if(building!=null){if(root.Ui.Interaction!=null)root.Ui.Interaction.OpenBuilding(building.InstanceId);else root.Select(building.InstanceId);return true;}
            return false;
        }
        public void Inspect(BuildingView site)
        {
            if(!inspection){savedTarget=target;savedZoom=zoom;savedDistrict=district;}
            inspection=true;inspectedBuilding=site;FrameInspection();
        }
        private void FrameInspection()
        {
            // BuildingView owns the current instance. A retired sprite may still be in the hierarchy
            // until end-of-frame Destroy runs, and must never determine the new framing.
            if(!inspectedBuilding.TryGetCurrentVisualBounds(out Bounds bounds))return;
            inspectionBounds=bounds;
            Vector3 up=renderCamera.transform.up,right=renderCamera.transform.right;
            float height=2*(Mathf.Abs(up.x)*bounds.extents.x+Mathf.Abs(up.y)*bounds.extents.y+Mathf.Abs(up.z)*bounds.extents.z);
            float width=2*(Mathf.Abs(right.x)*bounds.extents.x+Mathf.Abs(right.y)*bounds.extents.y+Mathf.Abs(right.z)*bounds.extents.z);
            zoom=Mathf.Max(8,height*.82f,width/(renderCamera.aspect*.82f));
            Vector3 groundUp=Vector3.ProjectOnPlane(up,Vector3.up).normalized;
            target=new Vector3(bounds.center.x,0,bounds.center.z)+groundUp*(bounds.center.y*up.y/Vector3.Dot(groundUp,up))-right*(.18f*2*zoom*renderCamera.aspect);
            inspectionLevel=root.Session.State.Find(inspectedBuilding.InstanceId).level;inspectionWidth=Screen.width;inspectionHeight=Screen.height;
        }
        public void EndInspection()
        {
            if(!inspection)return;
            inspection=false;inspectedBuilding=null;target=savedTarget;zoom=savedZoom;district=savedDistrict;
        }
        private void LeaveInspectionForGesture()
        {if(inspection){if(root.Ui.Interaction!=null)root.Ui.Interaction.CloseBuilding();else EndInspection();}}
        private void Update()
        {
            if(root==null)return;
            if(inspection&&(Screen.width!=inspectionWidth||Screen.height!=inspectionHeight||root.Session.State.Find(inspectedBuilding.InstanceId).level!=inspectionLevel||inspectedBuilding.TryGetCurrentVisualBounds(out var currentBounds)&&currentBounds!=inspectionBounds))FrameInspection();
            var input=EventSystem.current!=null&&EventSystem.current.currentSelectedGameObject!=null?EventSystem.current.currentSelectedGameObject.GetComponent<InputField>():null;
            bool typing=input!=null&&input.isActiveAndEnabled&&input.isFocused;
            if(!root.Ui.ModalOpen&&!typing&&root.Session!=null)
            {
                var keyboard=Keyboard.current;Vector2 keys=Vector2.zero;
                if(keyboard!=null)
                {
                    keys.x=(keyboard.dKey.isPressed||keyboard.rightArrowKey.isPressed?1:0)-(keyboard.aKey.isPressed||keyboard.leftArrowKey.isPressed?1:0);
                    keys.y=(keyboard.wKey.isPressed||keyboard.upArrowKey.isPressed?1:0)-(keyboard.sKey.isPressed||keyboard.downArrowKey.isPressed?1:0);
                    if(keyboard.equalsKey.isPressed){LeaveInspectionForGesture();zoom-=20*Time.unscaledDeltaTime;}
                    if(keyboard.minusKey.isPressed){LeaveInspectionForGesture();zoom+=20*Time.unscaledDeltaTime;}
                }
                if(keys.sqrMagnitude>0){LeaveInspectionForGesture();Pan(keys*panSpeed*Time.unscaledDeltaTime);}
                var touch=Touchscreen.current;
                int touches=0;UnityEngine.InputSystem.Controls.TouchControl first=null,second=null;
                if(touch!=null)foreach(var finger in touch.touches)if(finger.press.isPressed){if(first==null)first=finger;else if(second==null)second=finger;touches++;}
                if(touches>=2)
                {
                    mouseGesture.Cancel();touchGesture.Cancel();
                    if(!suppressTouchUntilRelease)suppressTouchUntilRelease=IsUiPoint(first.startPosition.ReadValue())||IsUiPoint(second.startPosition.ReadValue());
                    if(!suppressTouchUntilRelease)
                    {
                        LeaveInspectionForGesture();float distance=Vector2.Distance(first.position.ReadValue(),second.position.ReadValue());
                        if(pinchPrevious>0)zoom-=(distance-pinchPrevious)*zoom/Mathf.Max(100,Screen.height);
                        pinchPrevious=distance;
                    }
                }
                else if(touch!=null)
                {
                    // A remaining finger after a pinch must be lifted before it can become a new tap.
                    if(pinchPrevious>0){suppressTouchUntilRelease=true;pinchPrevious=0;}
                    var primary=touch.primaryTouch;float threshold=12;
                    if(primary.press.wasPressedThisFrame&&!suppressTouchUntilRelease)touchGesture.Begin(primary.position.ReadValue(),IsUiPoint(primary.position.ReadValue()));
                    if(primary.press.isPressed&&!suppressTouchUntilRelease)Drag(touchGesture.Move(primary.position.ReadValue(),threshold));
                    if(primary.press.wasReleasedThisFrame&&!suppressTouchUntilRelease&&touchGesture.End(primary.position.ReadValue(),threshold))TrySelect(primary.position.ReadValue());
                    if(touches==0){suppressTouchUntilRelease=false;pinchPrevious=0;}
                }
                var mouse=Mouse.current;
                if(mouse!=null&&touches==0)
                {
                    Vector2 point=mouse.position.ReadValue();
                    if(mouse.leftButton.wasPressedThisFrame)mouseGesture.Begin(point,IsUiPoint(point));
                    if(mouse.leftButton.isPressed)Drag(mouseGesture.Move(point,7));
                    if(mouse.leftButton.wasReleasedThisFrame&&mouseGesture.End(point,7))TrySelect(point);
                    if(!IsUiPoint(point))
                    {
                        float wheel=mouse.scroll.ReadValue().y;
                        if(Mathf.Abs(wheel)>.01f){LeaveInspectionForGesture();zoom-=wheel*.02f;}
                        if(mouse.middleButton.isPressed||mouse.rightButton.isPressed)Drag(mouse.delta.ReadValue());
                    }
                }
            }
            else {mouseGesture.Cancel();touchGesture.Cancel();suppressTouchUntilRelease=true;pinchPrevious=0;}
            if(!inspection)
            {
                zoom=Mathf.Clamp(zoom,minZoom,MaximumZoom);
                target=ClampToWorld(target);
            }
            transform.position=Vector3.Lerp(transform.position,target,1-Mathf.Exp(-8*Time.unscaledDeltaTime));
            renderCamera.orthographicSize=Mathf.Lerp(renderCamera.orthographicSize,zoom,1-Mathf.Exp(-8*Time.unscaledDeltaTime));
            if(districtMap!=null)
            {
                // Clamp the rendered view as well: zoom/position easing or an aspect change must never expose the outskirts.
                renderCamera.orthographicSize=Mathf.Min(renderCamera.orthographicSize,MaximumZoom);
                transform.position=ClampToWorld(transform.position,renderCamera.orthographicSize);
            }
        }
        private void Drag(Vector2 pixels)
        {if(pixels.sqrMagnitude<.001f)return;LeaveInspectionForGesture();Pan(-pixels*(2*zoom/Mathf.Max(1,Screen.height)));}
        private void Pan(Vector2 delta)
        {
            Vector3 right=renderCamera.transform.right,up=Vector3.ProjectOnPlane(renderCamera.transform.up,Vector3.up).normalized;
            Vector3 offset=right*delta.x+up*(delta.y/Vector3.Dot(up,renderCamera.transform.up));
            target=ClampToWorld(target+offset);
        }
        private float MaximumZoom => districtMap != null ? districtMap.MaximumNavigationZoom(renderCamera.aspect) : maxZoom;
        private float GroundProjection => Vector3.ProjectOnPlane(renderCamera.transform.up,Vector3.up).magnitude;
        private Vector3 ClampToWorld(Vector3 position) => ClampToWorld(position,zoom);
        private Vector3 ClampToWorld(Vector3 position,float viewZoom)
        {
            if(districtMap==null)return new Vector3(Mathf.Clamp(position.x,minimum.x,maximum.x),0,Mathf.Clamp(position.z,minimum.y,maximum.y));
            var plan=DistrictMap.ToPlan(position);var clamped=districtMap.ClampNavigation(plan,viewZoom,renderCamera.aspect,GroundProjection);
            // Avoid a lossy plane/world round trip every idle frame: the camera must remain still over UI.
            return clamped==plan?new Vector3(position.x,0,position.z):DistrictMap.ToWorld(clamped);
        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void SetDiagnosticZoom(float size){zoom=Mathf.Clamp(size,minZoom,MaximumZoom);renderCamera.orthographicSize=zoom;}
#endif
#if UNITY_EDITOR
        public void EditorConfigure(Camera camera){renderCamera=camera;}
        public void EditorSetDistrictMap(DistrictMap value){districtMap=value;}
        public void SetOverviewForEvidence(float size){SetDiagnosticZoom(size);}
#endif
    }
}
