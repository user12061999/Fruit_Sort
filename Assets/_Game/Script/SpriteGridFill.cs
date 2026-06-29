using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class SpriteGridFill : MonoBehaviour
{
    [SerializeField, Min(1)] private int columns = 5;
    [SerializeField, Min(1)] private int rows = 4;
    [SerializeField, Range(0f, 1f)] private float fillAmount = 1f;
    [SerializeField, Range(0f, 0.45f)] private float cellGap = 0.02f;

    private static readonly int ColumnsId = Shader.PropertyToID("_Columns");
    private static readonly int RowsId = Shader.PropertyToID("_Rows");
    private static readonly int FillAmountId = Shader.PropertyToID("_FillAmount");
    private static readonly int CellGapId = Shader.PropertyToID("_CellGap");
    private static readonly int LocalBoundsId = Shader.PropertyToID("_LocalBounds");

    private SpriteRenderer spriteRenderer;
    private MaterialPropertyBlock propertyBlock;
    private Sprite appliedSprite;
    private int appliedColumns = -1;
    private int appliedRows = -1;
    private float appliedFillAmount = -1f;
    private float appliedCellGap = -1f;

    public float FillAmount
    {
        get => fillAmount;
        set
        {
            fillAmount = Mathf.Clamp01(value);
            Apply(force: true);
        }
    }

    public int Columns
    {
        get => columns;
        set
        {
            columns = Mathf.Max(1, value);
            Apply(force: true);
        }
    }

    public int Rows
    {
        get => rows;
        set
        {
            rows = Mathf.Max(1, value);
            Apply(force: true);
        }
    }

    public float CellGap
    {
        get => cellGap;
        set
        {
            cellGap = Mathf.Clamp(value, 0f, 0.45f);
            Apply(force: true);
        }
    }

    public void SetGrid(int newColumns, int newRows)
    {
        columns = Mathf.Max(1, newColumns);
        rows = Mathf.Max(1, newRows);
        Apply(force: true);
    }

    /// <summary>World position ở tâm của một ô, theo đúng thứ tự trái->phải và dưới->trên.</summary>
    public Vector3 GetCellWorldPosition(int cellIndex)
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        Sprite sprite = spriteRenderer.sprite;
        if (sprite == null) return transform.position;

        int safeColumns = Mathf.Max(1, columns);
        int safeRows = Mathf.Max(1, rows);
        int safeIndex = Mathf.Clamp(cellIndex, 0, safeColumns * safeRows - 1);
        int row = safeIndex / safeColumns;
        int column = safeIndex % safeColumns;
        Bounds bounds = sprite.bounds;
        Vector3 local = new Vector3(
            bounds.min.x + (column + 0.5f) * bounds.size.x / safeColumns,
            bounds.min.y + (row + 0.5f) * bounds.size.y / safeRows,
            0f);
        return transform.TransformPoint(local);
    }

    private void OnEnable()
    {
        Apply(force: true);
    }

    private void OnValidate()
    {
        columns = Mathf.Max(1, columns);
        rows = Mathf.Max(1, rows);
        fillAmount = Mathf.Clamp01(fillAmount);
        cellGap = Mathf.Clamp(cellGap, 0f, 0.45f);
        Apply(force: true);
    }

    private void LateUpdate()
    {
        Apply(force: false);
    }

    private void OnDidApplyAnimationProperties()
    {
        Apply(force: true);
    }

    public void Refresh()
    {
        Apply(force: true);
    }

    private void Apply(bool force)
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        Sprite sprite = spriteRenderer.sprite;
        if (sprite == null)
            return;

        bool changed = force
            || sprite != appliedSprite
            || columns != appliedColumns
            || rows != appliedRows
            || !Mathf.Approximately(fillAmount, appliedFillAmount)
            || !Mathf.Approximately(cellGap, appliedCellGap);

        if (!changed)
            return;

        if (propertyBlock == null)
            propertyBlock = new MaterialPropertyBlock();

        Bounds bounds = sprite.bounds;
        Vector2 size = bounds.size;
        size.x = Mathf.Max(size.x, 0.00001f);
        size.y = Mathf.Max(size.y, 0.00001f);

        spriteRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(ColumnsId, columns);
        propertyBlock.SetFloat(RowsId, rows);
        propertyBlock.SetFloat(FillAmountId, fillAmount);
        propertyBlock.SetFloat(CellGapId, cellGap);
        propertyBlock.SetVector(
            LocalBoundsId,
            new Vector4(bounds.min.x, bounds.min.y, size.x, size.y));
        spriteRenderer.SetPropertyBlock(propertyBlock);

        appliedSprite = sprite;
        appliedColumns = columns;
        appliedRows = rows;
        appliedFillAmount = fillAmount;
        appliedCellGap = cellGap;
    }
}
