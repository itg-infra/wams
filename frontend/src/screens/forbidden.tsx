import { ShieldAlert, ArrowLeft, Home } from "lucide-react";
import { useNavigate } from "react-router-dom";
import { Button } from "../components/ui/button";

export default function Forbidden() {
  const navigate = useNavigate();

  return (
    <div className="flex min-h-screen items-center justify-center bg-gray-50 px-6">
      <div className="w-full max-w-lg rounded-2xl border border-gray-200 bg-white p-8 text-center shadow-sm">
        <div className="mx-auto flex h-20 w-20 items-center justify-center rounded-full bg-red-100">
          <ShieldAlert className="h-10 w-10 text-red-600" />
        </div>

        <p className="mt-6 text-5xl font-bold text-gray-900">403</p>

        <h1 className="mt-3 text-2xl font-semibold text-gray-900">
          Access Forbidden
        </h1>

        <p className="mt-4 text-sm leading-6 text-gray-600">
          You don't have permission to access this page.
          <br />
          Please contact your administrator if you believe this is a mistake.
        </p>

        <div className="mt-8 flex flex-col gap-3 sm:flex-row sm:justify-center">
          <Button
            id="btn_GoBack"
            variant="outline"
            onClick={() => navigate(-1)}
          >
            <ArrowLeft size={18} />
            Go Back
          </Button>

          <Button
            id="btn_GoHome"
            variant="destructive"
            onClick={() => navigate("/")}
          >
            <Home size={18} />
            Go to Home
          </Button>
        </div>

        <div className="mt-8 rounded-lg bg-gray-50 p-4 text-left">
          <p className="text-sm font-semibold text-gray-800">
            Why am I seeing this?
          </p>

          <ul className="mt-2 list-disc space-y-1 pl-5 text-sm text-gray-600">
            <li>Your account doesn't have permission for this feature.</li>
            <li>Your role may have changed.</li>
            <li>The page requires a higher access level.</li>
          </ul>
        </div>
      </div>
    </div>
  );
}
