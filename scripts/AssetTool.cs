namespace KinetixStudio { public static class AssetTool { public static void Main(string[] args) { try{Brand.Export(args[0]);}finally{T.DisposeFonts();} } } }
