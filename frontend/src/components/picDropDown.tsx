// import { useState, useEffect, useRef } from "react";
// import { ChevronDown, Search, Loader2 } from "lucide-react";
// import { useUserController } from "../controllers/masterData/userController";
// import type { User } from "../types/users.types";
// import type { WOPIC } from "../types/woPic";

// interface PICDropdownProps {
//   value: number | null;
//   onChange: (user: WOPIC | null) => void;
//   label?: string;
//   woID: number | string;
// }

// export function PICDropdown({
//   value,
//   onChange,
//   label = "PIC",
//   woID,
// }: PICDropdownProps) {
//   const { users, isLoading, handleSearch, fetchUsers } = useUserController();
//   const [open, setOpen] = useState(false);
//   const [search, setSearch] = useState("");
//   const ref = useRef<HTMLDivElement>(null);

//   useEffect(() => {
//     fetchUsers();
//   }, []);

//   // Close on outside click
//   useEffect(() => {
//     const handler = (e: MouseEvent) => {
//       if (ref.current && !ref.current.contains(e.target as Node)) {
//         setOpen(false);
//         setSearch("");
//       }
//     };
//     document.addEventListener("mousedown", handler);
//     return () => document.removeEventListener("mousedown", handler);
//   }, []);

//   const selectedUser = users.find((u) => u.id === value) ?? null;

//   const handleSearchChange = (e: React.ChangeEvent<HTMLInputElement>) => {
//     setSearch(e.target.value);
//     handleSearch(e.target.value);
//   };

//   const handleSelect = (e: React.MouseEvent, user: User) => {
//     e.preventDefault(); // Mencegah aksi bawaan browser
//     e.stopPropagation();
//     onChange(user);
//     setOpen(false);
//     setSearch("");
//   };

//   return (
//     <div ref={ref} className="relative">
//       <p className="text-[15px] font-semibold text-[#111827] mb-2">{label}</p>

//       {/* Trigger */}
//       <button
//         type="button"
//         onClick={() => setOpen((p) => !p)}
//         className="w-full h-10 rounded-[5px] border border-[#ffff] bg-[#ffff] px-3 flex items-center justify-between text-[14px] text-[#374151]"
//       >
//         <span className={selectedUser ? "text-[#374151]" : "text-[#9CA3AF]"}>
//           {selectedUser ? selectedUser.fullname : "Select PIC"}
//         </span>
//         <ChevronDown className="w-4 h-4 text-[#9CA3AF] shrink-0" />
//       </button>

//       {/* Dropdown */}
//       {open && (
//         <div className="absolute z-50 mt-1 w-full bg-white border border-[#ffff] rounded-[5px] shadow-md">
//           {/* Search */}
//           <div className="flex items-center gap-2 px-3 py-2 border-b border-[#ffff]">
//             <Search className="w-4 h-4 text-[#9CA3AF] shrink-0" />
//             <input
//               autoFocus
//               type="text"
//               value={search}
//               onChange={handleSearchChange}
//               placeholder="Search name..."
//               className="flex-1 text-[13px] outline-none bg-transparent text-[#374151] placeholder:text-[#9CA3AF]"
//             />
//           </div>

//           {/* List */}
//           <ul className="max-h-48 overflow-y-auto py-1">
//             {isLoading ? (
//               <li className="flex items-center gap-2 px-3 py-2 text-[13px] text-[#9CA3AF]">
//                 <Loader2 className="w-4 h-4 animate-spin" />
//                 Loading...
//               </li>
//             ) : users.length === 0 ? (
//               <li className="px-3 py-2 text-[13px] text-[#9CA3AF]">
//                 No users found
//               </li>
//             ) : (
//               users.map((user) => (
//                 <li
//                   key={user.id}
//                   onClick={(e) => handleSelect(e, user)}
//                   className={`px-3 py-2 cursor-pointer text-[14px] hover:bg-[#F5F5F5] transition ${
//                     user.id === value
//                       ? "bg-indigo-50 text-indigo-700 font-medium"
//                       : "text-[#374151]"
//                   }`}
//                 >
//                   <span>{user.fullname}</span>
//                   {user.employeeId && (
//                     <span className="ml-2 text-[12px] text-[#9CA3AF]">
//                       #{user.employeeId}
//                     </span>
//                   )}
//                 </li>
//               ))
//             )}
//           </ul>
//         </div>
//       )}
//     </div>
//   );
// }

import { useState, useEffect, useRef } from "react";
import { ChevronDown, Search, Loader2 } from "lucide-react";
import { useWorkOrderController } from "../controllers/operationalRealization/createWorkorderControllert";
import type { WOPIC } from "../types/woPic";

interface PICDropdownProps {
  value: number | null;
  onChange: (user: WOPIC | null) => void;
  label?: string;
  woID?: number | string | null; // 2. Tambahkan woID untuk fetch data
}

export function PICDropdown({
  value,
  onChange,
  label = "PIC",
  woID,
}: PICDropdownProps) {
  // 3. Gunakan controller yang baru kita buat
  const { pics, isLoading, fetchWOPICs } = useWorkOrderController();

  const [open, setOpen] = useState(false);
  const [search, setSearch] = useState("");
  const ref = useRef<HTMLDivElement>(null);

  // 4. Fetch data berdasarkan woID
  useEffect(() => {
    if (woID) {
      fetchWOPICs(woID);
    }
  }, [woID, fetchWOPICs]);

  // Close on outside click
  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) {
        setOpen(false);
        setSearch("");
      }
    };
    document.addEventListener("mousedown", handler);
    return () => document.removeEventListener("mousedown", handler);
  }, []);

  const selectedUser = pics.find((u) => u.id === value) ?? null;

  const handleSearchChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setSearch(e.target.value);
  };

  // 5. Filter data di frontend berdasarkan input search
  const filteredPics = pics.filter((pic) =>
    pic.fullname.toLowerCase().includes(search.toLowerCase()),
  );

  const handleSelect = (e: React.MouseEvent, user: WOPIC) => {
    e.preventDefault();
    e.stopPropagation(); // Mencegah form ke-refresh
    onChange(user);
    setOpen(false);
    setSearch("");
  };

  return (
    <div ref={ref} className="relative">
      <p className="text-[15px] font-semibold text-[#111827] mb-2">{label}</p>

      {/* Trigger */}
      <button
        type="button"
        onClick={() => setOpen((p) => !p)}
        className="w-full h-10 rounded-[5px] border border-[#ffff] bg-[#ffff] px-3 flex items-center justify-between text-[14px] text-[#374151]"
      >
        <span className={selectedUser ? "text-[#374151]" : "text-[#9CA3AF]"}>
          {selectedUser ? selectedUser.fullname : "Select PIC"}
        </span>
        <ChevronDown className="w-4 h-4 text-[#9CA3AF] shrink-0" />
      </button>

      {/* Dropdown */}
      {open && (
        <div className="absolute z-50 mt-1 w-full bg-white border border-[#ffff] rounded-[5px] shadow-md">
          {/* Search */}
          <div className="flex items-center gap-2 px-3 py-2 border-b border-[#ffff]">
            <Search className="w-4 h-4 text-[#9CA3AF] shrink-0" />
            <input
              autoFocus
              type="text"
              value={search}
              onChange={handleSearchChange}
              placeholder="Search name..."
              className="flex-1 text-[13px] outline-none bg-transparent text-[#374151] placeholder:text-[#9CA3AF]"
            />
          </div>

          {/* List */}
          <ul className="max-h-48 overflow-y-auto py-1">
            {isLoading ? (
              <li className="flex items-center gap-2 px-3 py-2 text-[13px] text-[#9CA3AF]">
                <Loader2 className="w-4 h-4 animate-spin" />
                Loading...
              </li>
            ) : filteredPics.length === 0 ? (
              <li className="px-3 py-2 text-[13px] text-[#9CA3AF]">
                No users found
              </li>
            ) : (
              // Gunakan filteredPics hasil pencarian
              filteredPics.map((user) => (
                <li
                  key={user.id}
                  onClick={(e) => handleSelect(e, user)}
                  className={`px-3 py-2 cursor-pointer text-[14px] hover:bg-[#F5F5F5] transition ${
                    user.id === value
                      ? "bg-indigo-50 text-indigo-700 font-medium"
                      : "text-[#374151]"
                  }`}
                >
                  <span>{user.fullname}</span>
                  {/* Bagian employeeId saya hapus karena di JSON API PIC tidak ada field employeeId */}
                </li>
              ))
            )}
          </ul>
        </div>
      )}
    </div>
  );
}