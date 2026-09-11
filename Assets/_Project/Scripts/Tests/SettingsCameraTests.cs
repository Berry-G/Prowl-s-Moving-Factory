using NUnit.Framework;
using UnityEngine;
using PMF.Session;

namespace PMF.Tests
{
    /// <summary>목적: 저장/카메라 경계 회귀. 구조: 실제 화면과 PlayerPrefs를 건드리지 않는 순수 검증. 불변: 사용자 설정 보존. 근거: ADR-0023.</summary>
    public sealed class SettingsCameraTests
    {
        [Test]
        public void SettingsJson_RetainsAllValues()
        {
            var source=new SettingsData {EdgeScrolling=false,Fps=165,VSync=true,Width=2560,Height=1440,Master=0.7f,Bgm=0.2f,Sfx=0.4f,Muted=true};
            var restored=UserSettings.Decode(JsonUtility.ToJson(source));
            Assert.IsFalse(restored.EdgeScrolling); Assert.AreEqual(165,restored.Fps);Assert.IsTrue(restored.VSync);
            Assert.AreEqual(2560,restored.Width);Assert.AreEqual(1440,restored.Height);
            Assert.AreEqual(0.7f,restored.Master);Assert.AreEqual(0.2f,restored.Bgm);Assert.AreEqual(0.4f,restored.Sfx);Assert.IsTrue(restored.Muted);
        }
        [TestCase(60)][TestCase(120)][TestCase(144)][TestCase(165)][TestCase(-1)]
        public void VSync_DoesNotForgetSelectedFps(int fps)
        {
            var s=new SettingsData{Fps=fps,VSync=true};s.Normalize();
            Assert.AreEqual(-1,s.EffectiveFps);Assert.AreEqual(fps,s.Fps);
            s.VSync=false;Assert.AreEqual(fps,s.EffectiveFps);
        }
        [Test]
        public void InvalidSettings_AreNormalized()
        {
            var s=new SettingsData{Fps=13,Width=-1,Height=999,Master=float.NaN,Bgm=-2,Sfx=9};s.Normalize();
            Assert.AreEqual(60,s.Fps);Assert.AreEqual(0,s.Width);Assert.AreEqual(0,s.Height);
            Assert.AreEqual(1,s.Master);Assert.AreEqual(0,s.Bgm);Assert.AreEqual(1,s.Sfx);
            Assert.DoesNotThrow(()=>UserSettings.Decode("broken json"));
        }
        [Test]
        public void Copy_DoesNotMutateSavedModel()
        {
            var a=new SettingsData();var b=a.Copy();b.Master=0;
            Assert.AreEqual(1,a.Master);
        }
        [Test]
        public void ShiftedWideMap_ClampsCameraAtVisibleEdges()
        {
            var b=new Bounds(new Vector3(100,50,0),new Vector3(80,40,0));
            Assert.AreEqual(new Vector3(70,35,-10),StageCameraController.ClampPosition(new Vector3(-999,-999,-10),b,5,2));
            Assert.AreEqual(new Vector3(130,65,-10),StageCameraController.ClampPosition(new Vector3(999,999,-10),b,5,2));
        }
        [Test]
        public void SmallMap_CentersWhenViewportIsLarger()
        {
            var b=new Bounds(new Vector3(10,20,0),new Vector3(4,3,0));
            Assert.AreEqual(new Vector3(10,20,-10),StageCameraController.ClampPosition(new Vector3(0,0,-10),b,8,2));
        }
        [Test]
        public void CameraInput_ZoomDragAndEdgeScrollAreClampedAndFrameRateIndependent()
        {
            var go=new GameObject("CameraTest",typeof(Camera));
            try
            {
                var cam=go.GetComponent<Camera>(); cam.orthographic=true; cam.pixelRect=new Rect(0,0,1000,600);
                cam.orthographicSize=10; go.transform.position=new Vector3(0,0,-10);
                var controller=go.AddComponent<StageCameraController>();
                var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
                System.Action<string,object> set=(n,v)=>typeof(StageCameraController).GetField(n,flags).SetValue(controller,v);
                set("_camera",cam);set("_bounds",new Bounds(Vector3.zero,new Vector3(100,100,0)));
                set("_aspect",cam.aspect);set("_fitSize",50f);set("_edgeEnabled",true);
                var method=typeof(StageCameraController).GetMethod("ApplyPointer",flags);
                System.Action<Vector2,float,bool,bool,float> input=(p,w,pressed,held,dt)=>method.Invoke(controller,new object[]{p,w,pressed,held,dt});
                var center=cam.pixelRect.center;
                input(center,120,false,false,0); Assert.Less(cam.orthographicSize,10);
                for(int i=0;i<100;i++)input(center,120,false,false,0);
                Assert.AreEqual(2,cam.orthographicSize,0.001f);
                for(int i=0;i<100;i++)input(center,-120,false,false,0);
                Assert.AreEqual(50,cam.orthographicSize,0.001f);
                cam.orthographicSize=5; go.transform.position=new Vector3(0,0,-10);
                var edge=new Vector2(999,300);
                for(int i=0;i<60;i++)input(edge,0,false,false,1f/60f);
                float at60=go.transform.position.x;
                go.transform.position=new Vector3(0,0,-10);
                for(int i=0;i<120;i++)input(edge,0,false,false,1f/120f);
                Assert.AreEqual(at60,go.transform.position.x,0.001f); Assert.AreEqual(12,at60,0.001f);
                set("_edgeEnabled",false); var before=go.transform.position;
                input(edge,0,false,false,1); Assert.AreEqual(before,go.transform.position);
                input(center,0,true,true,0);input(center+new Vector2(100,0),0,false,true,0);
                Assert.Less(go.transform.position.x,before.x);
                input(center+new Vector2(100000,0),0,false,true,0);
                Assert.AreEqual(-50+cam.orthographicSize*cam.aspect,go.transform.position.x,0.001f);
            }
            finally {Object.DestroyImmediate(go);}
        }

        [Test]
        public void EdgeScroll_DiagonalSpeedEqualsCardinalAndIgnoresOutside()
        {
            var rect=new Rect(10,10,1000,600);
            var diagonal=StageCameraController.EdgeDirection(new Vector2(11,11),rect,20);
            Assert.AreEqual(1,diagonal.magnitude,0.0001f);Assert.Less(diagonal.x,0);Assert.Less(diagonal.y,0);
            Assert.AreEqual(Vector2.zero,StageCameraController.EdgeDirection(new Vector2(5,5),rect,20));
            Assert.AreEqual(Vector2.zero,StageCameraController.EdgeDirection(rect.center,rect,20));
        }
    }
}
