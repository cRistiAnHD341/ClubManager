using ClubManager.Models;
using System.Collections.Generic;

namespace ClubManager.Services
{
    public interface IDataService
    {
        // Métodos para Abonados
        List<Abonado> GetAllAbonados();
        Abonado? GetAbonadoById(int id);
        bool AddAbonado(Abonado abonado);
        bool UpdateAbonado(Abonado abonado);
        bool DeleteAbonado(int id);

        // Métodos para Gestores
        List<Gestor> GetAllGestores();
        Gestor? GetGestorById(int id);

        // Métodos para Peñas
        List<Peña> GetAllPeñas();
        Peña? GetPeñaById(int id);

        // Métodos para Tipos de Abono
        List<TipoAbono> GetAllTiposAbono();
        TipoAbono? GetTipoAbonoById(int id);

        // Métodos adicionales que puedas necesitar
        bool SaveChanges();
    }
}