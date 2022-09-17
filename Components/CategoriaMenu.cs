using LanchesMac.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LanchesMac.Components;

public class CategoriaMenu : ViewComponent
{
    public CategoriaMenu(ICategoriaRepository categoriaRepository)
    {
        _categoriaRepository = categoriaRepository;
    }

    private readonly ICategoriaRepository _categoriaRepository;

    public IViewComponentResult Invoke()
    {
        var categorias = _categoriaRepository.Categorias.OrderBy(l => l.CategoriaNome);
        return View(categorias);
    }
}