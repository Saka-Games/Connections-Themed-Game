using UnityEngine;
using System.Collections.Generic;

public class TransportUnit : MonoBehaviour
{
    private List<Vector3> path;
    private int currentIdx = 0;
    private float speed = 100f; // cellSize=500 olduğu için hızlı olmalı (yaklaşık 5 saniyede 1 tarlayı geçer)
    
    private ProductionBuilding targetBuilding;
    private int amount;
    private GameObject visualSource; // Kamyonun asıl görseli (kaydırmak için)

    public string cargoType = "hammadde";

    public void Initialize(List<Vector3> route, ProductionBuilding target, int qty, GameObject truckPrefab = null)
    {
        path = route;
        targetBuilding = target;
        amount = qty;
        currentIdx = 0;
        
        if (path != null && path.Count > 0)
            transform.position = path[0];
            
        if (truckPrefab != null)
        {
            visualSource = Instantiate(truckPrefab, transform);
            visualSource.transform.localPosition = Vector3.zero;
            visualSource.transform.localRotation = Quaternion.identity;
            visualSource.transform.localScale = Vector3.one; 
        }
        else
        {
            // Fallback: Basit bir "Yük Kutusu" temsilcisi
            visualSource = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visualSource.transform.SetParent(this.transform);
            visualSource.transform.localPosition = Vector3.zero;
            visualSource.transform.localScale = new Vector3(8f, 8f, 15f);
            
            Renderer r = visualSource.GetComponent<Renderer>();
            if (r != null) r.material.color = new Color(1f, 0.5f, 0f);
        }
        
        // collider'ı kapatalım ki tıklamaları engellemesin
        foreach(var col in GetComponentsInChildren<Collider>()) col.enabled = false;
        
        // Yol katmanına al (2)
        SetLayerRecursive(gameObject, 2);
    }

    private void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform) SetLayerRecursive(child.gameObject, layer);
    }

    void Update()
    {
        if (path == null || currentIdx >= path.Count) return;

        Vector3 targetPos = path[currentIdx];
        Vector3 moveDir = (targetPos - transform.position).normalized;
        
        if (moveDir != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), Time.deltaTime * 10f);
            
            // --- ŞERİT AYRIMI (Lane Offset) ---
            // Gidiş yönünün sağına doğru 12 birim kaydır
            Vector3 rightDir = Vector3.Cross(Vector3.up, moveDir); 
            if (visualSource != null)
            {
                visualSource.transform.localPosition = Vector3.Lerp(visualSource.transform.localPosition, rightDir * 12f, Time.deltaTime * 5f);
            }
        }

        transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPos) < 1f)
        {
            currentIdx++;
            if (currentIdx >= path.Count)
            {
                OnArrive();
            }
        }
    }

    private void OnArrive()
    {
        if (targetBuilding != null)
        {
            // FABRİKA ÖZEL DURUMU: Malzemeyi doğru ambar slotuna koy
            if (targetBuilding.binaTipi == ProductionBuilding.BuildingType.Fabrika)
            {
                if (cargoType == "sut") targetBuilding.mevcutSut += amount;
                else if (cargoType == "aroma") targetBuilding.mevcutAroma += amount;
                else if (cargoType == "kulah") targetBuilding.mevcutKulah += amount;
                else targetBuilding.currentRawMaterial += amount; // Kimyasal vb.
            }
            else
            {
                // Diğer binalar (Fırın, Ahıl, Şehir) genel hammadde havuzunu kullanır
                targetBuilding.currentRawMaterial += amount;
            }

            Debug.Log($"<color=orange>[LOGISTICS]:</color> {amount} birim {cargoType} {targetBuilding.binaAdi} binasına ulaştı.");
        }
        Destroy(gameObject);
    }
}
