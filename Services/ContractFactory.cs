using ContractBotApi.Models;

public class ContractFactory
{
    public static Contract CreateContract(string contractType)
    {
        switch (contractType.ToLower())
        {
            case "spot contract":
                return new SpotContract();
            case "forward contract":
                return new ForwardContract();
            case "option contract":
                return new OptionContract();
            case "swap contract":
                return new SwapContract();
            default:
                throw new ArgumentException("Invalid contract type");
        }
    }
}