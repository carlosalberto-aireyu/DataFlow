using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace DataFlow.UI.Helpers
{
    public static class EditTransactionHelper
    {
        public static bool TryCommitOrCancel(ICollectionView view, bool preferCommit = true)
        {
            if (view is null) return true;

            if (view is IEditableCollectionView editable)
            {
                if (editable.IsAddingNew)
                {
                    if (preferCommit)
                    {
                        try
                        {
                            editable.CommitNew();
                        }
                        catch
                        {
                            try { editable.CancelNew(); }
                            catch (Exception ex) { Debug.WriteLine($"Fallo al cancelar un nuevo item: {ex}"); }
                            return false;
                        }
                    }
                    else
                    {
                        editable.CancelNew();
                        return true;
                    }

                }
                else if (editable.IsEditingItem)
                {
                    if (preferCommit)
                    {
                        try
                        {
                            editable.CommitEdit();
                        }
                        catch
                        {
                            try { editable.CancelEdit(); }
                            catch (Exception ex) { Debug.WriteLine($"Fallo al cancelar la edicion de un item: {ex}"); }
                            return false;
                        }
                    }
                    else
                    {
                        editable.CancelEdit();
                        return true;
                    }
                }
            }
            return true;
        }

        public static bool TryCommitOrCancel(DataGrid grid, ICollectionView view, bool preferCommit = true)
        {
            if (grid is not null)
            {
                try
                {
                    bool cellCommited = grid.CommitEdit(DataGridEditingUnit.Cell, true);
                    bool rowCommited = grid.CommitEdit(DataGridEditingUnit.Row, true);
                    if (!cellCommited || !rowCommited)
                    {
                        if (!preferCommit)
                        {
                            grid.CancelEdit(DataGridEditingUnit.Cell);
                            grid.CancelEdit(DataGridEditingUnit.Row);
                        }
                        else
                        {
                            grid.CancelEdit(DataGridEditingUnit.Cell);
                            grid.CancelEdit(DataGridEditingUnit.Row);
                            return false;
                        }
                    }
                }
                catch
                {
                    try
                    {
                        grid.CancelEdit(DataGridEditingUnit.Cell);
                        grid.CancelEdit(DataGridEditingUnit.Row);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Fallo al cancelar la edicion de un item en DataGrid: {ex}");
                        return false;
                    }
                }
            }

            return TryCommitOrCancel(view, preferCommit);
        }

        public static bool TryCommitOrCancel(DataGrid grid, bool preferCommit = true)
        {
            if (grid is null) return true;

            try
            {
                if (preferCommit)
                {

                    bool cellCommitted = grid.CommitEdit(DataGridEditingUnit.Cell, true);
                    bool rowCommitted = grid.CommitEdit(DataGridEditingUnit.Row, true);

                    if (!cellCommitted || !rowCommitted)
                    {
                        grid.CancelEdit(DataGridEditingUnit.Cell);
                        grid.CancelEdit(DataGridEditingUnit.Row);
                        return false;
                    }
                    return true;
                }
                else
                {

                    grid.CancelEdit(DataGridEditingUnit.Cell);
                    grid.CancelEdit(DataGridEditingUnit.Row);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en TryCommitOrCancel para DataGrid: {ex.Message}");
                try
                {

                    grid.CancelEdit(DataGridEditingUnit.Cell);
                    grid.CancelEdit(DataGridEditingUnit.Row);
                }
                catch (Exception cancelEx)
                {
                    Debug.WriteLine($"Error al intentar cancelar después de fallo: {cancelEx.Message}");
                }
                return false;
            }
        }

        public static bool TryCommitOrCancelMultiple(bool preferCommit, params DataGrid?[] grids)
        {
            if (grids == null || grids.Length == 0) return true;

            bool allSuccess = true;
            foreach (var grid in grids)
            {
                if (grid != null)
                {
                    bool result = TryCommitOrCancel(grid, preferCommit);
                    if (!result)
                    {
                        allSuccess = false;
                    }
                }
            }
            return allSuccess;
        }
    }
}