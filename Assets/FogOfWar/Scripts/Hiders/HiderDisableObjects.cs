using UnityEngine;

namespace FOW
{
    public class HiderDisableObjects : HiderBehavior
    {
        [SerializeField] public GameObject[] ObjectsToHide;

        protected override void OnHide()
        {
            foreach (GameObject o in ObjectsToHide)
                if (o != null) {
                    o.SetActive(false);
                }
        }

        protected override void OnReveal()
        {
            foreach (GameObject o in ObjectsToHide)
                if(o != null) {
                    o.SetActive(true);
                }
        }

        public void ModifyHiddenObjects(GameObject[] newObjectsToHide)
        {
            OnReveal();
            ObjectsToHide = newObjectsToHide;
            if (!enabled)
                return;

            if (!IsEnabled)
                OnHide();
            else
                OnReveal();
        }
    }
}