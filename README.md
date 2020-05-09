# FIDO2AuthenticationMechanismImplementation

Realizuota pasitelkiant https://github.com/abergs/fido2-net-lib 

## Norint pasibandyti... ##

1. Susikurti duomenų bazę ir lenteles. [Kaip tai padaryti...](https://github.com/herkusas/FIDO2AuthenticationMechanismImplementation/tree/master/dbScripts)
2. Įsirašyti Elasticsearch ir Kibana arba naudoti elastic cloud (free 14 dienų, no credit card required) tik nepamirškit development settingsuose nurodyti urlą tada kitą ir appendint basic auth startupe prie settings. Parsisiųsti Elasticsearch[čia](https://www.elastic.co/guide/en/elasticsearch/reference/current/windows.html) ir [Kibana](https://artifacts.elastic.co/downloads/kibana/kibana-7.6.2-windows-x86_64.zip). Kibana siūlau įsirašinėti kaip servisą su [NSSM](https://nssm.cc/release/nssm-2.24.zip) * Jeigu neįsirašysit šitų vis tiek turėtų veikti, tik, kad nesiųs eventų, nedariau, kad throwintų jeigu neįmanoma pasiekti elastic...
3. [3.1.* .NET Core SDK](https://dotnet.microsoft.com/download/dotnet-core/thank-you/sdk-3.1.201-windows-x64-installer), kad susibuildinti solutioną... Pageidautiną taip pat turėtį kokį nors IDE, jeigu kartais pas Jus naudojami portai bus užimti ir reikės juos pasikoreguoti per nustatymus... Na, bet visad tam galima naudoti notepad.
4. Beabejo turėti FIDO2 autentifikacijos standartą palaikantį autentifkatorių. Pvz.: iš [Yubico](https://www.yubico.com/)
5. Turbūt viskas tada. Jeigu ką herkus-vaigaudas.gaidanis@stud.vgtu.lt ...
