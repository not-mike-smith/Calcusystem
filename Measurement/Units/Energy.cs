using Measurement.BaseClasses;
using Measurement.Factories;
using Measurement.Models;

namespace Measurement.Units;

public class Energy : ReflectiveUnitList<Energy>
{
    public static readonly Energy Units = new();
    private Energy() { }

    // SI m-kg-s
    public static readonly UnitOfMeasure Joule = UnitFactory.Create(
        "J",
        (Force.Newton, 1),
        (Length.Meter, 1));

    public static readonly UnitOfMeasure Millijoule = Metric.m(Joule);
    public static readonly UnitOfMeasure Microjoule = Metric.micro(Joule);
    public static readonly UnitOfMeasure Nanojoule = Metric.n(Joule);
    public static readonly UnitOfMeasure Picojoule = Metric.p(Joule);
    public static readonly UnitOfMeasure Femtojoule = Metric.f(Joule);
    public static readonly UnitOfMeasure Attojoule = Metric.a(Joule);
    public static readonly UnitOfMeasure Zeptojoule = Metric.z(Joule);
    public static readonly UnitOfMeasure Yoctojoule = Metric.y(Joule);

    public static readonly UnitOfMeasure Kilojoule = Metric.k(Joule);
    public static readonly UnitOfMeasure Megajoule = Metric.M(Joule);
    public static readonly UnitOfMeasure Gigajoule = Metric.G(Joule);
    public static readonly UnitOfMeasure Terajoule = Metric.T(Joule);
    public static readonly UnitOfMeasure Petajoule = Metric.P(Joule);
    public static readonly UnitOfMeasure Exajoule = Metric.E(Joule);
    public static readonly UnitOfMeasure Zettajoule = Metric.Z(Joule);
    public static readonly UnitOfMeasure Yottajoule = Metric.Y(Joule);

    // SI cm-g-s
    public static readonly UnitOfMeasure Erg = UnitFactory.Create("erg", 1E-7, Joule);
    public static readonly UnitOfMeasure MilliErg = Metric.m(Erg);
    public static readonly UnitOfMeasure MicroErg = Metric.micro(Erg);
    public static readonly UnitOfMeasure NanoErg = Metric.n(Erg);
    public static readonly UnitOfMeasure PicoErg = Metric.p(Erg);
    public static readonly UnitOfMeasure FemtoErg = Metric.f(Erg);
    public static readonly UnitOfMeasure AttoErg = Metric.a(Erg);
    public static readonly UnitOfMeasure ZeptoErg = Metric.z(Erg);
    public static readonly UnitOfMeasure YoctoErg = Metric.y(Erg);

    public static readonly UnitOfMeasure KiloErg = Metric.k(Erg);
    public static readonly UnitOfMeasure MegaErg = Metric.M(Erg);
    public static readonly UnitOfMeasure GigaErg = Metric.G(Erg);
    public static readonly UnitOfMeasure TeraErg = Metric.T(Erg);
    public static readonly UnitOfMeasure PetaErg = Metric.P(Erg);
    public static readonly UnitOfMeasure ExaErg = Metric.E(Erg);
    public static readonly UnitOfMeasure ZettaErg = Metric.Z(Erg);
    public static readonly UnitOfMeasure YottaErg = Metric.Y(Erg);

    // Atomic
    public static readonly UnitOfMeasure ElectronVolt = UnitFactory.Create(
        "eV",
        1d / ElectricCharge.ElectronsInCoulomb,
        Joule);

    public static readonly UnitOfMeasure MilliElectronVolt = Metric.m(ElectronVolt);
    public static readonly UnitOfMeasure MicroElectronVolt = Metric.micro(ElectronVolt);
    public static readonly UnitOfMeasure NanoElectronVolt = Metric.n(ElectronVolt);
    public static readonly UnitOfMeasure PicoElectronVolt = Metric.p(ElectronVolt);
    public static readonly UnitOfMeasure FemtoElectronVolt = Metric.f(ElectronVolt);
    public static readonly UnitOfMeasure AttoElectronVolt = Metric.a(ElectronVolt);
    public static readonly UnitOfMeasure ZeptoElectronVolt = Metric.z(ElectronVolt);
    public static readonly UnitOfMeasure YoctoElectronVolt = Metric.y(ElectronVolt);

    public static readonly UnitOfMeasure KiloElectronVolt = Metric.k(ElectronVolt);
    public static readonly UnitOfMeasure MegaElectronVolt = Metric.M(ElectronVolt);
    public static readonly UnitOfMeasure GigaElectronVolt = Metric.G(ElectronVolt);
    public static readonly UnitOfMeasure TeraElectronVolt = Metric.T(ElectronVolt);
    public static readonly UnitOfMeasure PetaElectronVolt = Metric.P(ElectronVolt);
    public static readonly UnitOfMeasure ExaElectronVolt = Metric.E(ElectronVolt);
    public static readonly UnitOfMeasure ZettaElectronVolt = Metric.Z(ElectronVolt);
    public static readonly UnitOfMeasure YottaElectronVolt = Metric.Y(ElectronVolt);

    public static readonly UnitOfMeasure KilowattHour = UnitFactory.Create(
        "kWh",
        (Power.Kilowatt, 1),
        (Time.Hour, 1));

    public static readonly UnitOfMeasure MegawattHour = UnitFactory.Create(
        "MWh",
        (Power.Megawatt, 1),
        (Time.Hour, 1));

    public static readonly UnitOfMeasure GigawattHour = UnitFactory.Create(
        "GWh",
        (Power.Gigawatt, 1),
        (Time.Hour, 1));

    public static readonly UnitOfMeasure TerawattHour = UnitFactory.Create(
        "TWh",
        (Power.Terawatt, 1),
        (Time.Hour, 1));

    // English Engineering
    public static readonly UnitOfMeasure FootPound = UnitFactory.Create(
        (Length.Foot, 1),
        (Force.PoundForce, 1));

    // BTUs (https://physics.nist.gov/cuu/pdf/sp811.pdf, pg. 46)
    public static readonly UnitOfMeasure BtuThermochemical = UnitFactory.Create("Btuₜₕ", 1.055055852d, Kilojoule);
    public static readonly UnitOfMeasure BtuInternational = UnitFactory.Create("Btu(IT)", 1.054350, Kilojoule);
    public static readonly UnitOfMeasure BtuMean = UnitFactory.Create("Btu(mean)", 1.05587, Kilojoule);
    public static readonly UnitOfMeasure Btu39 = UnitFactory.Create("Btu(39°F)", 1.05967, Kilojoule);
    public static readonly UnitOfMeasure Btu59 = UnitFactory.Create("Btu(59°F)", 1.05480, Kilojoule);
    public static readonly UnitOfMeasure Btu60 = UnitFactory.Create("Btu(60°F)", 1.05468, Kilojoule);

    public static readonly UnitOfMeasure MBtuThermochemical = Metric.ThousandM(BtuThermochemical);
    public static readonly UnitOfMeasure MBtuInternational = Metric.ThousandM(BtuInternational);
    public static readonly UnitOfMeasure MBtuMean = Metric.ThousandM(BtuMean);
    public static readonly UnitOfMeasure MBtu39 = Metric.ThousandM(Btu39);
    public static readonly UnitOfMeasure MBtu59 = Metric.ThousandM(Btu59);
    public static readonly UnitOfMeasure MBtu60 = Metric.ThousandM(Btu60);

    public static readonly UnitOfMeasure MMBtuThermochemical = Metric.MM(BtuThermochemical);
    public static readonly UnitOfMeasure MMBtuInternational = Metric.MM(BtuInternational);
    public static readonly UnitOfMeasure MMBtuMean = Metric.MM(BtuMean);
    public static readonly UnitOfMeasure MMBtu39 = Metric.MM(Btu39);
    public static readonly UnitOfMeasure MMBtu59 = Metric.MM(Btu59);
    public static readonly UnitOfMeasure MMBtu60 = Metric.MM(Btu60);

    // calorie (https://physics.nist.gov/cuu/pdf/sp811.pdf, pg. 47)
    public static readonly UnitOfMeasure CalorieThermochemical = UnitFactory.Create("calₜₕ", 4.184, Joule);
    public static readonly UnitOfMeasure CalorieInternational = UnitFactory.Create("cal(IT)", 4.1868, Joule);
    public static readonly UnitOfMeasure CalorieMean = UnitFactory.Create("cal(mean)", 4.19002, Joule);
    public static readonly UnitOfMeasure Calorie20 = UnitFactory.Create("cal(15°C)", 4.18580, Joule);
    public static readonly UnitOfMeasure Calorie15 = UnitFactory.Create("cal(20°C)", 4.18190, Joule);

    public static readonly UnitOfMeasure NutritionalCalorieThermochemical = UnitFactory.Create("Calₜₕ", 4.184, Joule);
    public static readonly UnitOfMeasure NutritionalCalorieInternational = UnitFactory.Create("Cal(IT)", 4.1868, Joule);
    public static readonly UnitOfMeasure NutritionalCalorieMean = UnitFactory.Create("Cal(mean)", 4.19002, Joule);
    public static readonly UnitOfMeasure NutritionalCalorie20 = UnitFactory.Create("Cal(15°C)", 4.18580, Joule);
    public static readonly UnitOfMeasure NutritionalCalorie15 = UnitFactory.Create("Cal(20°C)", 4.18190, Joule);
}
