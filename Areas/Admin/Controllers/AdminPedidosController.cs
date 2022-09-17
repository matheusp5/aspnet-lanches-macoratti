using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LanchesMac.Context;
using LanchesMac.Models;
using LanchesMac.ViewModels;
using ReflectionIT.Mvc.Paging;
using SendGrid;
using SendGrid.Helpers;
using SendGrid.Helpers.Mail;

namespace LanchesMac.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AdminPedidos : Controller
    {
        private readonly AppDbContext _context;

        public AdminPedidos(AppDbContext context)
        {
            _context = context;
        }

        // GET: Admin/AdminPedidos
        /*public async Task<IActionResult> Index()
        {
            return View(await _context.Pedidos.ToListAsync());
        }*/
        public async Task<IActionResult> Index(string filter, int pageindex = 1, string sort = "Nome")
        {
            var resultado = _context.Pedidos
                .AsNoTracking()
                .AsQueryable();
            if (!string.IsNullOrWhiteSpace(filter))
            {
                resultado = resultado.Where(p => p.Nome.Contains(filter));
            }

            var model = await PagingList.CreateAsync(resultado, 5, pageindex, sort, "Nome");
            model.RouteValue = new RouteValueDictionary { { "filter", filter } };
            return View(model);
        }

        // GET: Admin/AdminPedidos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pedido = await _context.Pedidos
                .FirstOrDefaultAsync(m => m.PedidoId == id);
            if (pedido == null)
            {
                return NotFound();
            }

            return View(pedido);
        }

        // GET: Admin/AdminPedidos/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/AdminPedidos/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PedidoId,Nome,Sobrenome,Endereco1,Endereco2,Cep,Estado,Cidade,Telefone,Email,PedidoEnviado,PedidoEntregueEm")] Pedido pedido)
        {
            if (ModelState.IsValid)
            {
                _context.Add(pedido);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(pedido);
        }

        // GET: Admin/AdminPedidos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido == null)
            {
                return NotFound();
            }
            return View(pedido);
        }

        // POST: Admin/AdminPedidos/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PedidoId,Nome,Sobrenome,Endereco1,Endereco2,Cep,Estado,Cidade,Telefone,Email,PedidoEnviado,PedidoEntregueEm")] Pedido pedido)
        {
            if (id != pedido.PedidoId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(pedido);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PedidoExists(pedido.PedidoId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(pedido);
        }

        // GET: Admin/AdminPedidos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pedido = await _context.Pedidos
                .FirstOrDefaultAsync(m => m.PedidoId == id);
            if (pedido == null)
            {
                return NotFound();
            }

            return View(pedido);
        }

        // POST: Admin/AdminPedidos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var pedido = await _context.Pedidos.FindAsync(id);
            _context.Pedidos.Remove(pedido);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PedidoExists(int id)
        {
            return _context.Pedidos.Any(e => e.PedidoId == id);
        }

        public IActionResult PedidoLanche(int? id)
        {
            var pedido = _context.Pedidos
                .Include(l => l.PedidoItens)
                .ThenInclude(pd => pd.Lanche)
                .FirstOrDefault();

            if (pedido == null)
            {
                Response.StatusCode = 404;
                return View("PedidoNotFound", id.Value);
            }


            PedidoLancheViewModel pedidoLanche = new PedidoLancheViewModel()
            {
                Pedido = pedido,
                PedidoDetalhes = pedido.PedidoItens
            };

            return View(PedidoLanche);
        }


        public IActionResult Entrega(int id)
        {
            if (id != null)
            {
                var pedido = _context.Pedidos.FirstOrDefault(pd => pd.PedidoId.Equals(id));
                if (pedido != null)
                {
                    pedido.PedidoEntregueEm = DateTime.Now;
                    _context.SaveChanges();

                    SendEmail(pedido.Email, pedido.Nome);
                }
            }
            return RedirectToAction("Index");
        }

        static async Task SendEmail(string clientMail, string clientName)
        {
            var apiKey = "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";
            var client = new SendGridClient(apiKey);
            var from = new EmailAddress("contato@mxtheuz.com.br", "Lanches Piccoli");
            var subject = "Aooba, prepare a sua mesa =)";
            var to = new EmailAddress(clientMail, clientName);
            var plainTextContent = "Pode ir se preparando pois nosso entregador ja esta a caminho de sua casa com o seu lanche!";
            var htmlContent = "<strong>Pode ir se preparando pois nosso entregador ja esta a caminho de sua casa com o seu lanche!</strong>";
            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            var response = await client.SendEmailAsync(msg);
        }
    }
}
