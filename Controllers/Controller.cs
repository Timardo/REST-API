using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace REST_API.Controllers
{
    [Route("api/")]
    [ApiController]
    public class Controller : ControllerBase
    {
        private readonly DatabaseContext DbContext;

        public Controller(DatabaseContext context)
        {
            DbContext = context;
        }

        #region Employees

        // api/employees
        [HttpGet("employees")]
        public async Task<ActionResult<IEnumerable<Employee>>> GetEmployees()
        {
            return await DbContext.Employees.ToListAsync();
        }

        // api/employees/{id}
        [HttpGet("employees/{id}")]
        public async Task<ActionResult<Employee>> GetEmployee(int id)
        {
            Employee employee = await DbContext.Employees.FindAsync(id);

            if (employee == null)
                return NotFound();

            return employee;
        }

        // api/employees/{id}
        [HttpPut("employees/{id}")]
        public async Task<IActionResult> PutEmployee(int id, Employee employee)
        {
            if (id != employee.Id)
                return BadRequest("Id v PUT requeste sa nezhoduje s id ktoré sa pokúšate aktualizovať - id nemožno zmeniť.");

            ObjectResult result = DoChecks(employee); // kontrola kódu uzlu a vedúceho

            if (result != null)
                return result;

            DbContext.Entry(employee).State = EntityState.Modified;

            try
            {
                await DbContext.SaveChangesAsync();
            }

            catch (DbUpdateConcurrencyException)
            {
                if (!EmployeeExists(id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }

        // api/employees
        [HttpPost("employees")]
        public async Task<ActionResult<Employee>> PostEmployee(Employee employee)
        {
            ObjectResult result = DoChecks(employee); // kontrola kódu uzlu a vedúceho

            if (result != null)
                return result;

            if (await DbContext.Employees.AnyAsync())
                employee.Id = DbContext.Employees.Max(x => x.Id) + 1; // automaticky zvyšujúce sa ID
            else
                employee.Id = 1; // v prípade, že je databáza prázdna

            DbContext.Employees.Add(employee);
            await DbContext.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEmployee), new { id = employee.Id }, employee);
        }

        // api/employees/{id}
        [HttpPatch("employees/{id}")]
        public async Task<ActionResult<Employee>> PatchEmployee(int id, Employee pE) // pE - patchEmployee
        {
            ObjectResult result = DoChecks(pE); // kontrola kódu uzlu a vedúceho

            if (result != null)
                return result;

            Employee oE = await DbContext.Employees.FindAsync(id); // oE - oldEmployee

            if (oE == null)
                return NotFound();

            // nenulové hodnoty v requeste nahradia hodnoty v databáze
            oE.Name = pE.Name ?? oE.Name;
            oE.Surname = pE.Surname ?? oE.Surname;
            oE.Phone = pE.Phone ?? oE.Phone;
            oE.Email = pE.Email ?? oE.Email;
            oE.Manager = pE.Manager == -1 ? oE.Manager : pE.Manager;
            oE.Code = pE.Code ?? oE.Code;

            DbContext.Entry(oE).State = EntityState.Modified;

            try
            {
                await DbContext.SaveChangesAsync();
            }

            catch (DbUpdateConcurrencyException)
            {
                if (!EmployeeExists(id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }

        // api/employees/{id}
        [HttpDelete("employees/{id}")]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            Employee employee = await DbContext.Employees.FindAsync(id);

            if (employee == null)
                return NotFound();

            DbContext.Employees.Remove(employee);
            await DbContext.SaveChangesAsync();

            return NoContent();
        }

        private bool EmployeeExists(int id)
        {
            return DbContext.Employees.Any(e => e.Id == id);
        }

        private ObjectResult DoChecks(Employee employee)
        {
            if (employee.Code != null && !DbContext.Nodes.Any(n => n.Code == employee.Code))
                return Problem("Databáza nemá záznam o uzle s kódom: (" + employee.Code + ").");

            if (employee.Manager == 1 && DbContext.Employees.Any(e => e.Code == employee.Code && e.Manager == 1))
                return Problem("Uzol: (" + employee.Code + ") už má vedúceho. Uzol nemôže mať viac ako jedného vedúceho.");

            return null;
        }

        #endregion Employees
        // #######################################################################################
        #region Nodes

        // api/nodes
        [HttpGet("nodes")]
        public async Task<ActionResult<IEnumerable<Node>>> GetNodes()
        {
            return await DbContext.Nodes.ToListAsync();
        }

        // api/nodes/{id}
        [HttpGet("nodes/{id}")]
        public async Task<ActionResult<Node>> GetNode(int id)
        {
            Node node = await DbContext.Nodes.FindAsync(id);

            if (node == null)
                return NotFound();

            return node;
        }

        // api/nodes/{id}
        [HttpPut("nodes/{id}")]
        public async Task<IActionResult> PutNode(int id, Node node)
        {
            if (id != node.Id)
                return BadRequest("Id v PUT requeste sa nezhoduje s id ktoré sa pokúšate aktualizovať - id nemožno zmeniť");

            ObjectResult result = CheckCodes(node);

            if (result != null)
                return result;
            
            Node oldNode = await DbContext.Nodes.FindAsync(node.Id);

            if (node.Code != oldNode.Code) // ak sa mení kód uzlu, treba updatovať jeho deti a zamestnancov
            {
                UpdateCodes(oldNode.Code, node.Code);
            }

            if (id == 1) // firma môže mať jedine id 1
                node.ParentCode = null;

            DbContext.Entry(node).State = EntityState.Modified;

            try
            {
                await DbContext.SaveChangesAsync();
            }

            catch (DbUpdateConcurrencyException)
            {
                if (!NodeExists(id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }

        // api/nodes
        [HttpPost("nodes")]
        public async Task<ActionResult<Node>> PostNode(Node node)
        {
            if (node.Code == null)
                return Problem("Kód uzlu je povinný");

            if (await DbContext.Nodes.CountAsync() != 0)
            {
                if (node.ParentCode == null)
                    return Problem("Kód materského uzlu je povinný ak už existuje záznam firmy");

                ObjectResult result = CheckCodes(node);

                if (result != null)
                    return result;

                node.Id = DbContext.Nodes.Max(x => x.Id) + 1; // automaticky zvyšujúce sa ID
            }

            else
            {
                node.ParentCode = null; // prvý záznam musí byť firma -> bez materského uzlu
                node.Id = 1; // v prípade, že je databáza prázdna
            }

            DbContext.Nodes.Add(node);
            await DbContext.SaveChangesAsync();

            return CreatedAtAction(nameof(GetNode), new { id = node.Id }, node);
        }

        // api/nodes/{id}
        [HttpPatch("nodes/{id}")]
        public async Task<ActionResult<Node>> PatchNode(int id, Node pN) // pE - patchNode
        {
            Node oN = await DbContext.Nodes.FindAsync(id); // oN - oldNode

            if (oN == null)
                return NotFound();

            ObjectResult result = CheckCodes(pN);

            if (result != null)
                return result;

            // nenulové hodnoty v requeste nahradia hodnoty v databáze
            oN.Name = pN.Name ?? oN.Name;
            oN.Code = pN.Code ?? oN.Code;
            oN.ParentCode = pN.ParentCode ?? oN.ParentCode;

            if (id == 1) // firma môže mať jedine id 1
                oN.ParentCode = null;

            if (pN.Code != oN.Code) // ak sa mení kód uzlu, treba updatovať jeho deti a zamestnancov
            {
                UpdateCodes(oN.Code, pN.Code);
            }

            DbContext.Entry(oN).State = EntityState.Modified;

            try
            {
                await DbContext.SaveChangesAsync();
            }

            catch (DbUpdateConcurrencyException)
            {
                if (!NodeExists(id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }

        // api/nodes/{id}
        [HttpDelete("nodes/{id}")]
        public async Task<IActionResult> DeleteNode(int id)
        {
            Node node = await DbContext.Nodes.FindAsync(id);

            if (node == null)
                return NotFound();

            List<Node> nodes = new List<Node> { node };

            while (nodes.Count != 0) // odstráni všetky uzly pod tým, čo chceme vymazať
            {
                Node node1 = nodes.First();
                nodes.Remove(node1);
                DbContext.Nodes.Remove(node1);

                IQueryable<Employee> employees = DbContext.Employees.Where(e => e.Code == node1.Code);

                foreach (Employee employee in employees)
                {
                    employee.Code = null;
                    DbContext.Entry(employee).State = EntityState.Modified;
                }

                nodes.AddRange(DbContext.Nodes.Where(n => n.ParentCode == node1.Code));
            }

            await DbContext.SaveChangesAsync();

            return NoContent();
        }

        private bool NodeExists(int id)
        {
            return DbContext.Nodes.Any(e => e.Id == id);
        }

        private ObjectResult CheckCodes(Node node)
        {
            if (node.ParentCode != null && !DbContext.Nodes.Any(n => n.Code == node.ParentCode))
                return Problem("Uzol s materským kódom: (" + node.ParentCode + ") neexistuje");

            if (node.Code != null && DbContext.Nodes.Any(n => n.Code == node.Code))
                return Problem("Uzol s kódom: (" + node.Code + ") už existuje");

            return null;
        }

        private void UpdateCodes(string oldCode, string newCode)
        {
            IQueryable<Node> childNodes = DbContext.Nodes.Where(n => n.ParentCode == oldCode);

            foreach (Node node1 in childNodes)
            {
                node1.ParentCode = newCode;
                DbContext.Entry(node1).State = EntityState.Modified;
            }

            IQueryable<Employee> employees = DbContext.Employees.Where(e => e.Code == oldCode);

            foreach (Employee employee in employees)
            {
                employee.Code = newCode;
                DbContext.Entry(employee).State = EntityState.Modified;
            }
        }
        #endregion Nodes
    }
}
