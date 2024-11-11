using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TicTacToe;
using Mirror;
using UnityEngine.UIElements;

public class StageManager : MonoBehaviour
{
    [SerializeField] MatchController _matchController;
    [SerializeField] GameObject _templateStagePrefab; 
    GameObject _currentStageObject;

    public MatchInfo matchInfo;

    // cells logic
    [SerializeField] GameObject _cellPrefab, _cellParent;
    public List<CellScript> cells;

    // markers
    public List<GameObject> markerPrefabs;


    [ClientCallback]
    public void OnStartClient()
    {
        _currentStageObject = Instantiate(_templateStagePrefab);
        var stageScript = _currentStageObject.GetComponentInChildren<StageVisualScript>();
        stageScript.CreateGridLines(matchInfo.gridSize);

        CreateCellLogic();
    }

    private void Update()
    {
        if(Input.touchCount > 0)
        {
            var touch = Input.GetTouch(0);
            if(touch.phase == TouchPhase.Began)
            {
                Ray ray = Camera.main.ScreenPointToRay(touch.position);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit))
                {
                    CellScript cellTouched;
                    if (hit.collider.gameObject.TryGetComponent<CellScript>(out cellTouched))
                    {
                        Debug.Log($"Touched: {cellTouched.cellValue}");
                        cellTouched.OnCellTouched();
                    }
                }
            }
        }
    }

    [ClientCallback]
    public void CreateCellLogic()
    {
        _cellParent = _currentStageObject.GetComponentInChildren<CellContainer>().gameObject;

        float padding = 0.5f;
        float stageLength = 15.0f;
        float lineWidth = matchInfo.gridSize >= 7 ? .05f : 0.2f;

        Vector3 _topLeftCorner = new Vector3(-0.5f, 0.01f, 0.5f);

        float dimension = (stageLength - (2 * padding) - ((matchInfo.gridSize - 1) * lineWidth)) / matchInfo.gridSize;
        Debug.Log($"Dimension: {dimension}");
        float offsetScaled = ((padding) + (dimension / 2)) / stageLength;
        Debug.Log($"Row Offset: {offsetScaled}");
        float dimensionScaled = dimension / stageLength;
        float widthScaled = lineWidth / stageLength;

        float rowOffset = offsetScaled;

        for (int row = 0; row < matchInfo.gridSize; row++)
        {
            float columnOffset = offsetScaled;

            for(int col = 0; col < matchInfo.gridSize; col++)
            {
                var currentCellObject = Instantiate(_cellPrefab, _cellParent.transform);
                currentCellObject.transform.localPosition = _topLeftCorner + new Vector3(columnOffset, 0f, - rowOffset);
                Debug.Log($"Position: {currentCellObject.transform.position}");
                currentCellObject.transform.localScale = new Vector3(offsetScaled, 0.01f, offsetScaled);

                var currentCellScript = currentCellObject.GetComponent<CellScript>();
                currentCellScript.matchController = _matchController;
                currentCellScript.cellValue = (row * matchInfo.gridSize) + col;
                cells.Add(currentCellScript);

                columnOffset += dimensionScaled + widthScaled;
            }

            rowOffset += dimensionScaled + widthScaled;
        }
    }

    public void PutMarker(int cellValue, int playerNumber)
    {
        float padding = 0.5f;
        float stageLength = 15.0f;
        float lineWidth = matchInfo.gridSize >= 7 ? .05f : 0.2f;

        float dimension = (stageLength - (2 * padding) - ((matchInfo.gridSize - 1) * lineWidth)) / matchInfo.gridSize;
        float offsetScaled = ((padding) + (dimension / 2)) / stageLength;

        var cellScript = cells[cellValue];
        var marker = Instantiate(markerPrefabs[playerNumber], _cellParent.transform.parent);
        marker.transform.localScale = new Vector3(offsetScaled, .2f, offsetScaled);
        marker.transform.position = cellScript.transform.position;
    }
}
