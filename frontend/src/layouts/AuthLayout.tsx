import { Outlet, Navigate } from 'react-router-dom'
import { Anchor, Ship } from 'lucide-react'
import { useAuthStore } from '@/store/authStore'

export default function AuthLayout() {
  const { isAuthenticated } = useAuthStore()

  if (isAuthenticated) {
    return <Navigate to="/dashboard" replace />
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-navy-900 via-navy-800 to-teal-900 flex">
      {/* Left Panel - Branding */}
      <div className="hidden lg:flex lg:w-1/2 flex-col justify-center items-center p-12 relative overflow-hidden">
        {/* Background Pattern */}
        <div className="absolute inset-0 opacity-10">
          <div className="absolute top-20 left-10 w-40 h-40 border-4 border-white rounded-full"></div>
          <div className="absolute bottom-32 right-20 w-32 h-32 border-4 border-white rounded-full"></div>
          <div className="absolute top-1/2 left-1/4 w-24 h-24 border-4 border-white rounded-full"></div>
        </div>
        
        {/* Content */}
        <div className="relative z-10 text-center text-white">
          <div className="flex justify-center mb-8">
            <div className="bg-white/10 backdrop-blur-sm p-6 rounded-full">
              <Ship className="w-20 h-20 text-teal-400" />
            </div>
          </div>
          
          <h1 className="text-4xl font-bold mb-4">
            Pelindo Car Loan
          </h1>
          <p className="text-xl text-gray-300 mb-8">
            Sistem Peminjaman Kendaraan Operasional
          </p>
          
          <div className="flex items-center justify-center gap-2 text-gray-400">
            <Anchor className="w-5 h-5" />
            <span>PT Pelabuhan Indonesia (Persero)</span>
          </div>

          {/* Features */}
          <div className="mt-12 grid grid-cols-2 gap-4 text-left">
            <div className="bg-white/5 backdrop-blur-sm p-4 rounded-lg">
              <h3 className="font-semibold text-teal-400">Efisien</h3>
              <p className="text-sm text-gray-400">Proses pengajuan dan persetujuan yang cepat</p>
            </div>
            <div className="bg-white/5 backdrop-blur-sm p-4 rounded-lg">
              <h3 className="font-semibold text-teal-400">Transparan</h3>
              <p className="text-sm text-gray-400">Pantau status pengajuan secara real-time</p>
            </div>
            <div className="bg-white/5 backdrop-blur-sm p-4 rounded-lg">
              <h3 className="font-semibold text-teal-400">Terintegrasi</h3>
              <p className="text-sm text-gray-400">Sistem terpadu untuk semua divisi</p>
            </div>
            <div className="bg-white/5 backdrop-blur-sm p-4 rounded-lg">
              <h3 className="font-semibold text-teal-400">Audit Trail</h3>
              <p className="text-sm text-gray-400">Jejak aktivitas yang tercatat dengan baik</p>
            </div>
          </div>
        </div>
      </div>

      {/* Right Panel - Login Form */}
      <div className="w-full lg:w-1/2 flex items-center justify-center p-8">
        <div className="w-full max-w-md">
          {/* Mobile Logo */}
          <div className="lg:hidden text-center mb-8">
            <div className="inline-flex items-center justify-center bg-white/10 backdrop-blur-sm p-4 rounded-full mb-4">
              <Ship className="w-12 h-12 text-teal-400" />
            </div>
            <h1 className="text-2xl font-bold text-white">Pelindo Car Loan</h1>
          </div>

          {/* Form Container */}
          <div className="bg-white rounded-2xl shadow-2xl p-8">
            <Outlet />
          </div>

          {/* Footer */}
          <p className="text-center text-gray-400 text-sm mt-8">
            © {new Date().getFullYear()} PT Pelabuhan Indonesia (Persero). All rights reserved.
          </p>
        </div>
      </div>
    </div>
  )
}
