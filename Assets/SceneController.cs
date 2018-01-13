using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneController : MonoBehaviour {
	public GameObject genericPopupBody;
	public GameObject genericPopup;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public void ShowProjects() {
		genericPopup.SetActive(true);
		GameObject menu = Instantiate (Resources.Load ("Menus/ProjectsMenu")) as GameObject;
		menu.transform.SetParent (genericPopupBody.transform, false);
	}
	public void ShowEquipment() {
		genericPopup.SetActive(true);
		GameObject menu = Instantiate (Resources.Load ("Menus/EquipmentMenu")) as GameObject;
		menu.transform.SetParent (genericPopupBody.transform, false);
	}
	public void ShowPeople() {
		genericPopup.SetActive(true);
		GameObject menu = Instantiate (Resources.Load ("Menus/PeopleMenu")) as GameObject;
		menu.transform.SetParent (genericPopupBody.transform, false);
	}
	public void ShowMaterials() {
		genericPopup.SetActive(true);
		GameObject menu = Instantiate (Resources.Load ("Menus/MaterialsMenu")) as GameObject;
		menu.transform.SetParent (genericPopupBody.transform, false);
	}
	public void ShowPayroll() {
		genericPopup.SetActive(true);
		GameObject menu = Instantiate (Resources.Load ("Menus/PayrollMenu")) as GameObject;
		menu.transform.SetParent (genericPopupBody.transform, false);
	}
	public void ShowDailyReports() {
		genericPopup.SetActive(true);
		GameObject menu = Instantiate (Resources.Load ("Menus/DailyReportsMenu")) as GameObject;
		menu.transform.SetParent (genericPopupBody.transform, false);
	}
	public void ShowEstimates() {
		genericPopup.SetActive(true);
		GameObject menu = Instantiate (Resources.Load ("Menus/EstimatesMenu")) as GameObject;
		menu.transform.SetParent (genericPopupBody.transform, false);
	}
	public void ShowBilling() {
		genericPopup.SetActive(true);
		GameObject menu = Instantiate (Resources.Load ("Menus/BillingMenu")) as GameObject;
		menu.transform.SetParent (genericPopupBody.transform, false);
	}
	public void ShowHelp() {
		genericPopup.SetActive(true);
		GameObject menu = Instantiate (Resources.Load ("Menus/HelpMenu")) as GameObject;
		menu.transform.SetParent (genericPopupBody.transform, false);
	}
	public void PopupBack() {
		foreach (Transform child in genericPopupBody.transform) {
			Destroy(child.gameObject);
		}
	}
}
