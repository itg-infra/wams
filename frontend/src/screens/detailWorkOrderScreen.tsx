import { Clock3, Construction } from "lucide-react";
import { useNavigate } from "react-router-dom";
import { PageHeader } from "../components/ui/page-header";

export default function DetailWorkOrderScreen() {
  const navigate = useNavigate();

  return (
    <div className="flex-1 p-4 sm:p-6 lg:p-8 overflow-y-auto">

      <PageHeader title="Detail Work Order" onBack={() => navigate(-1)} />

      <div
        className="
          flex
          min-h-[70vh]
          items-center
          justify-center
        "
      >
        <div
          className="
            w-full
            max-w-160
            rounded-4xl
            border
            border-[#ececec]
            bg-white
            p-10
            shadow-sm
          "
        >
          {/* ICON */}

          <div className="mb-6 flex justify-center">
            <div
              className="
                flex
                h-24
                w-24
                items-center
                justify-center
                rounded-full
                bg-[#f3f0ff]
              "
            >
              <Construction size={42} className="text-[#3f2b96]" />
            </div>
          </div>

          {/* TITLE */}

          <div className="mb-4 text-center">
            <h2 className="mb-2 text-[28px] font-bold text-[#202224]">
              Under Development
            </h2>

            <p
              className="
                mx-auto
                max-w-120
                text-[14px]
                leading-6
                text-[#7c7c88]
              "
            >
              We are currently building the Work Order detail page to provide a
              better operational experience. Please check back later.
            </p>
          </div>

          {/* STATUS */}

          <div
            className="
              mt-8
              flex
              items-center
              justify-center
            "
          >
            <div
              className="
                inline-flex
                items-center
                gap-2
                rounded-full
                border
                border-[#ddd6fe]
                bg-[#f5f3ff]
                px-4
                py-2
              "
            >
              <Clock3 size={15} className="text-[#7c3aed]" />

              <span
                className="
                  text-[12px]
                  font-semibold
                  text-[#7c3aed]
                "
              >
                Feature In Progress
              </span>
            </div>
          </div>

          {/* ACTION */}

          <div className="mt-10 flex justify-center">
            <button
              id="btn_BackWorkOrder"
              type="button"
              onClick={() => navigate(-1)}
              className="
                rounded-[10px]
                bg-[#3f2b96]
                px-6
                py-3
                text-[13px]
                font-semibold
                text-white
                transition-all
                hover:bg-[#2f2173]
              "
            >
              Back to Work Orders
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
