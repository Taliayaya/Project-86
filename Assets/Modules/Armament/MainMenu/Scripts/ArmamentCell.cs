using Armament.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Armament.MainMenu
{
	public class ArmamentCell : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
	{
		[SerializeField] private GameObject SelectedBorder;
		[SerializeField] private GameObject HoverBorder;
		[SerializeField] private Image Icon;
		[SerializeField] private TextMeshProUGUI Text;
		[SerializeField] private TextMeshProUGUI[] Details;


		private ArmamentSO _armamentSo;
		private bool _isSelected;
		private bool _isHover;
		private bool IsSelected => ArmamentConfigManager.GetConfig().CurrentArmament == _armamentSo.ArmamentType;

		private bool Selected
		{
			get => _isSelected;
			set
			{
				_isSelected = value;
				SelectedBorder.SetActive(_isSelected);
			}
		}

		private bool Hovered
		{
			get => _isHover;
			set
			{
				_isHover = value;
				HoverBorder.SetActive(_isHover);
			}
		}

		private void OnEnable()
		{
			MenuEvents.Instance.OnChangedArmament += OnChangedArmament;
		}

		private void OnDisable()
		{
			MenuEvents.Instance.OnChangedArmament -= OnChangedArmament;
		}

		private void OnChangedArmament()
		{
			Selected = IsSelected;
		}

		public void Init(ArmamentSO so)
		{
			_armamentSo = so;
			Icon.sprite = _armamentSo.Icon;
			Text.text = _armamentSo.Name;
			ShowDetails();

			Selected = IsSelected;
		}
		private void ShowDetails()
		{
			int maxDetails = Mathf.Min(Details.Length, _armamentSo.Details.Length);
			for (var i = 0; i < Details.Length; i++)
			{
				if (maxDetails <= i)
				{
					Details[i].gameObject.SetActive(false);
				}
				else
				{
					Details[i].gameObject.SetActive(true);
					int value1 = _armamentSo.Details[i].Value;
					int value2 = _armamentSo.Details[i].Value;
					string detailValue = value1 != value2 ? $"{Mathf.Min(value1, value2)}-{Mathf.Max(value1, value2)}" : value1.ToString();

					string detailName = _armamentSo.Details[i].Name;

					Details[i].text = $"{detailName}: {detailValue}";
				}
			}
		}


		public void OnClick()
		{
			if (Selected) return;
			ArmamentConfigManager.GetConfig().SetArmament(_armamentSo);
			ArmamentConfigManager.Instance.Save();
			Selected = true;
			MenuEvents.Instance.FireOnChangedArmament();
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			Hovered = true;
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			Hovered = false;
		}
		public void OnPointerClick(PointerEventData eventData)
		{
			OnClick();
		}
	}
}